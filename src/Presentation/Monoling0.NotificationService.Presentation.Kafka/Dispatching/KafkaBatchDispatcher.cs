using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monoling0.NotificationService.Messaging.Headers;
using Monoling0.NotificationService.Messaging.Kafka;
using Monoling0.NotificationService.Messaging.Kafka.Models;
using Monoling0.NotificationService.Messaging.Kafka.Options;
using Monoling0.NotificationService.Persistence.Models.Inbox;
using Monoling0.NotificationService.Persistence.Repositories;
using Monoling0.NotificationService.UseCases;
using System.Collections.Concurrent;

namespace Monoling0.NotificationService.Presentation.Kafka.Dispatching;

public sealed class KafkaBatchDispatcher<T> : BackgroundService
{
    private readonly IKafkaBatchSource<T> _source;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IKafkaDlqProducer _deadLetterQueueProducer;
    private readonly KafkaDlqOptions _deadLetterQueueOptions;
    private readonly KafkaProcessingOptions _processingOptions;
    private readonly ILogger<KafkaBatchDispatcher<T>> _logger;

    public KafkaBatchDispatcher(
        IKafkaBatchSource<T> source,
        IServiceScopeFactory scopeFactory,
        IKafkaDlqProducer deadLetterQueueProducer,
        IOptions<KafkaDlqOptions> deadLetterQueueOptions,
        IOptions<KafkaProcessingOptions> processingOptions,
        ILogger<KafkaBatchDispatcher<T>> logger)
    {
        _source = source;
        _scopeFactory = scopeFactory;
        _deadLetterQueueProducer = deadLetterQueueProducer;
        _deadLetterQueueOptions = deadLetterQueueOptions.Value;
        _processingOptions = processingOptions.Value;
        _logger = logger;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _source.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _source.StartAsync(stoppingToken);

        await foreach (KafkaConsumeBatch<T> batch in _source.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessBatchAsync(batch, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(KafkaConsumeBatch<T> batch, CancellationToken cancellationToken)
    {
        if (batch.Messages.Count == 0)
        {
            await batch.CommitAsync(cancellationToken);
            return;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();

        IInboxRepository inbox = scope.ServiceProvider.GetRequiredService<IInboxRepository>();
        IBatchEventHandler<T>? batchHandler = scope.ServiceProvider.GetService<IBatchEventHandler<T>>();
        IEventHandler<T>? singleHandler = scope.ServiceProvider.GetService<IEventHandler<T>>();

        IReadOnlyList<KafkaConsumedMessage<T>> accepted = await AcquireInboxAsync(
            inbox,
            batch.Messages,
            cancellationToken);

        if (accepted.Count == 0)
        {
            await batch.CommitAsync(cancellationToken);
            return;
        }

        IReadOnlyCollection<KafkaConsumedMessage<T>> succeeded = accepted;
        IReadOnlyCollection<FailedMessage<T>> failed = [];

        try
        {
            if (batchHandler is not null)
            {
                await batchHandler.HandleBatchAsync(accepted, cancellationToken);
            }
            else if (singleHandler is not null)
            {
                (succeeded, failed) = await HandleIndividuallyAsync(
                    singleHandler,
                    accepted,
                    cancellationToken);
            }
            else
            {
                throw new InvalidOperationException($"No handler registered for {typeof(T).Name}.");
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to handle kafka batch for {EventType}.", typeof(T).Name);

            if (singleHandler is not null)
            {
                (succeeded, failed) = await HandleIndividuallyAsync(
                    singleHandler,
                    accepted,
                    cancellationToken);
            }
            else
            {
                succeeded = [];
                failed = accepted.Select(message => new FailedMessage<T>(message, exception)).ToArray();
            }
        }

        if (succeeded.Count > 0)
            await MarkProcessedAsync(inbox, succeeded, cancellationToken);

        if (failed.Count > 0)
            await MarkFailedAsync(inbox, failed, cancellationToken);

        if (failed.Count == 0 || _processingOptions.CommitOnError)
            await batch.CommitAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<KafkaConsumedMessage<T>>> AcquireInboxAsync(
        IInboxRepository inbox,
        IReadOnlyCollection<KafkaConsumedMessage<T>> messages,
        CancellationToken cancellationToken)
    {
        var accepted = new ConcurrentBag<KafkaConsumedMessage<T>>();
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _processingOptions.MaxParallelism,
            CancellationToken = cancellationToken,
        };

        await Parallel.ForEachAsync(messages, options, async (message, token) =>
        {
            var position = new InboxEventPosition(
                message.Position.Topic.Value,
                message.Position.Partition,
                message.Position.Offset);

            InboxDecision decision = await inbox.TryAcquireAsync(message.EventId, position, token);

            if (decision == InboxDecision.Accepted)
                accepted.Add(message);
        });

        return accepted.ToArray();
    }

    private async Task<(
        IReadOnlyCollection<KafkaConsumedMessage<T>> ConsumedMessages,
        IReadOnlyCollection<FailedMessage<T>> FailedMessages)> HandleIndividuallyAsync(
        IEventHandler<T> handler,
        IReadOnlyCollection<KafkaConsumedMessage<T>> messages,
        CancellationToken cancellationToken)
    {
        var succeeded = new ConcurrentBag<KafkaConsumedMessage<T>>();
        var failed = new ConcurrentBag<FailedMessage<T>>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _processingOptions.MaxParallelism,
            CancellationToken = cancellationToken,
        };

        await Parallel.ForEachAsync(messages, options, async (message, token) =>
        {
            try
            {
                await handler.HandleAsync(message, token);
                succeeded.Add(message);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to handle kafka message {EventId}.", message.EventId);
                failed.Add(new FailedMessage<T>(message, exception));
            }
        });

        return (succeeded.ToArray(), failed.ToArray());
    }

    private async Task MarkProcessedAsync(
        IInboxRepository inbox,
        IEnumerable<KafkaConsumedMessage<T>> messages,
        CancellationToken cancellationToken)
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _processingOptions.MaxParallelism,
            CancellationToken = cancellationToken,
        };

        await Parallel.ForEachAsync(messages, options, (message, token) =>
            inbox.MarkAsProcessedAsync(message.EventId, token));
    }

    private async Task MarkFailedAsync(
        IInboxRepository inbox,
        IEnumerable<FailedMessage<T>> messages,
        CancellationToken cancellationToken)
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _processingOptions.MaxParallelism,
            CancellationToken = cancellationToken,
        };

        await Parallel.ForEachAsync(messages, options, async (message, token) =>
        {
            if (message.Message.EventId != null)
                await inbox.MarkAsFailedAsync(message.Message.EventId, message.Exception.Message, token);
            await PublishDeadLetterQueueAsync(message.Message, message.Exception, token);
        });
    }

    private Task PublishDeadLetterQueueAsync(
        KafkaConsumedMessage<T> message,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (!_deadLetterQueueOptions.Enabled || !_processingOptions.PublishToDlqOnError)
            return Task.CompletedTask;

        var headers = new MessageHeaders(message.Headers);
        headers.WithUtf8("x-error", exception.Message);
        headers.WithUtf8("x-error-type", exception.GetType().Name);

        var deadLetterQueueTopic = new KafkaTopicName(
            message.Position.Topic.Value + _deadLetterQueueOptions.TopicSuffix);

        return _deadLetterQueueProducer.PublishAsync(
            deadLetterQueueTopic,
            message.Key,
            message.Payload ?? [],
            headers,
            message.Position,
            exception.ToString(),
            cancellationToken);
    }

    private sealed record FailedMessage<TMessage>(KafkaConsumedMessage<TMessage> Message, Exception Exception);
}
