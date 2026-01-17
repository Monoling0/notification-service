using Common;
using Confluent.Kafka;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monoling0.NotificationService.Messaging.Headers;
using Monoling0.NotificationService.Messaging.Kafka;
using Monoling0.NotificationService.Messaging.Kafka.Metadata;
using Monoling0.NotificationService.Messaging.Kafka.Models;
using Monoling0.NotificationService.Messaging.Kafka.Options;
using Monoling0.NotificationService.Observability;
using Monoling0.NotificationService.Presentation.Kafka.Configuration;
using Monoling0.NotificationService.Presentation.Kafka.Headers;
using System.Diagnostics;
using System.Threading.Channels;

namespace Monoling0.NotificationService.Presentation.Kafka.Batching;

public sealed class KafkaBatchSource<T> : IKafkaBatchSource<T>, IAsyncDisposable where T : class, IMessage<T>, new()
{
    private readonly Channel<KafkaConsumeBatch<T>> _channel;
    private readonly Channel<CommitRequest> _commitChannel;
    private readonly IKafkaSerializer<T> _serializer;
    private readonly IKafkaEnvelopeMetadataAccessor<T> _metadataAccessor;
    private readonly ITracingContextAccessor _tracingContextAccessor;
    private readonly KafkaConsumerOptions _consumerOptions;
    private readonly KafkaBatchingOptions _batchingOptions;
    private readonly KafkaClientOptions _clientOptions;
    private readonly KafkaDlqOptions _deadLetterQueueOptions;
    private readonly IKafkaDlqProducer _deadLetterQueueProducer;
    private readonly KafkaTopicName _topic;
    private readonly ILogger<KafkaBatchSource<T>> _logger;
    private Task? _consumerTask;
    private CancellationTokenSource? _cancellationTokenSource;

    public KafkaBatchSource(
        KafkaTopicName topic,
        IKafkaSerializer<T> serializer,
        IKafkaEnvelopeMetadataAccessor<T> metadataAccessor,
        ITracingContextAccessor tracingContextAccessor,
        IOptions<KafkaConsumerOptions> consumerOptions,
        IOptions<KafkaBatchingOptions> batchingOptions,
        IOptions<KafkaClientOptions> clientOptions,
        IOptions<KafkaDlqOptions> deadLetterQueueOptions,
        IKafkaDlqProducer deadLetterQueueProducer,
        ILogger<KafkaBatchSource<T>> logger)
    {
        _topic = topic;
        _serializer = serializer;
        _metadataAccessor = metadataAccessor;
        _tracingContextAccessor = tracingContextAccessor;
        _consumerOptions = consumerOptions.Value;
        _batchingOptions = batchingOptions.Value;
        _clientOptions = clientOptions.Value;
        _deadLetterQueueOptions = deadLetterQueueOptions.Value;
        _deadLetterQueueProducer = deadLetterQueueProducer;
        _logger = logger;

        var channelOptions = new BoundedChannelOptions(_batchingOptions.ChannelCapacity)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = _batchingOptions.WaitWhenChannelFull
                ? BoundedChannelFullMode.Wait
                : BoundedChannelFullMode.DropOldest,
        };

        _channel = Channel.CreateBounded<KafkaConsumeBatch<T>>(channelOptions);
        _commitChannel = Channel.CreateUnbounded<CommitRequest>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    }

    public ChannelReader<KafkaConsumeBatch<T>> Reader => _channel.Reader;

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        if (_consumerTask is not null)
            return ValueTask.CompletedTask;

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _consumerTask = Task.Run(
            () => ConsumeLoopAsync(_cancellationTokenSource.Token),
            CancellationToken.None);

        return ValueTask.CompletedTask;
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource is null)
            return;

        _cancellationTokenSource.Cancel();

        if (_consumerTask is not null)
            await _consumerTask.WaitAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_cancellationTokenSource is null)
            return;

        _cancellationTokenSource.Cancel();

        try
        {
            if (_consumerTask is not null)
                await _consumerTask;
        }
        finally
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private static List<TopicPartitionOffset> BuildCommitOffsets(IEnumerable<BatchItem> batch)
    {
#pragma warning disable SK1500
        var latest = new Dictionary<TopicPartition, long>();
#pragma warning restore SK1500

        foreach (BatchItem item in batch)
        {
            TopicPartitionOffset topicPartitionOffset = item.Offset;
            long offset = topicPartitionOffset.Offset.Value;

            if (!latest.TryGetValue(topicPartitionOffset.TopicPartition, out long currentOffset) ||
                offset > currentOffset)
            {
                latest[topicPartitionOffset.TopicPartition] = offset;
            }
        }

        return latest
            .Select(item => new TopicPartitionOffset(item.Key, item.Value + 1))
            .ToList();
    }

    private static int ToIntOffset(long offset)
    {
        if (offset > int.MaxValue)
            return int.MaxValue;

        if (offset < int.MinValue)
            return int.MinValue;

        return (int)offset;
    }

    private async Task ConsumeLoopAsync(CancellationToken cancellationToken)
    {
        ConsumerConfig config = KafkaConfigBuilder.BuildConsumer(_consumerOptions, _clientOptions);
        using IConsumer<string, byte[]> consumer = new ConsumerBuilder<string, byte[]>(config).Build();

        consumer.Subscribe(_topic.Value);

        var batch = new List<BatchItem>(_batchingOptions.MaxBatchSize);
        var batchTimer = Stopwatch.StartNew();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                DrainCommitRequests(consumer);

                TimeSpan timeout = _batchingOptions.BatchTimeout - batchTimer.Elapsed;
                if (timeout < TimeSpan.Zero)
                    timeout = TimeSpan.Zero;

                ConsumeResult<string, byte[]>? consumeResult = null;

                try
                {
                    consumeResult = consumer.Consume(timeout);
                }
                catch (ConsumeException exception)
                {
                    _logger.LogError(exception, "Kafka consume error on {Topic}.", _topic);
                }

                if (consumeResult is null)
                {
                    if (batch.Count > 0 && batchTimer.Elapsed >= _batchingOptions.BatchTimeout)
                    {
                        await FlushAsync(batch, cancellationToken);
                        batchTimer.Restart();
                    }

                    continue;
                }

                if (consumeResult.IsPartitionEOF)
                    continue;

                BatchItem? item = await TryBuildItemAsync(consumeResult, cancellationToken);
                if (item is not null)
                    batch.Add(item);

                if (batch.Count >= _batchingOptions.MaxBatchSize)
                {
                    await FlushAsync(batch, cancellationToken);
                    batchTimer.Restart();
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (batch.Count > 0)
            {
                try
                {
                    await FlushAsync(batch, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to flush final batch for {Topic}.", _topic);
                }
            }

            _channel.Writer.TryComplete();
            consumer.Close();
        }
    }

    private void DrainCommitRequests(IConsumer<string, byte[]> consumer)
    {
        while (_commitChannel.Reader.TryRead(out CommitRequest? request))
        {
            try
            {
                consumer.Commit(request.Offsets);
                request.Completion.TrySetResult(true);
            }
            catch (Exception exception)
            {
                request.Completion.TrySetException(exception);
            }
        }
    }

    private async Task FlushAsync(List<BatchItem> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
            return;

        IReadOnlyList<TopicPartitionOffset> commitOffsets = BuildCommitOffsets(batch);
        KafkaConsumedMessage<T>[] messages = batch
            .Where(item => item.Message is not null)
            .Select(item => item.Message ?? throw new ArgumentNullException(nameof(item)))
            .ToArray();

        var consumeBatch = new KafkaConsumeBatch<T>(messages, token => CommitAsync(commitOffsets, token));

        await _channel.Writer.WriteAsync(consumeBatch, cancellationToken);

        batch.Clear();
    }

    private async Task CommitAsync(IReadOnlyList<TopicPartitionOffset> offsets, CancellationToken cancellationToken)
    {
        if (offsets.Count == 0)
            return;

        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await _commitChannel.Writer.WriteAsync(new CommitRequest(offsets, completion), cancellationToken);
        await completion.Task.WaitAsync(cancellationToken);
    }

    private async Task<BatchItem?> TryBuildItemAsync(
        ConsumeResult<string, byte[]> result,
        CancellationToken cancellationToken)
    {
        byte[] payload = result.Message?.Value ?? [];

        T envelope;
        try
        {
            envelope = _serializer.Deserialize(payload);
        }
        catch (Exception exception)
        {
            await PublishDeserializationErrorAsync(result, payload, exception, cancellationToken);
            return new BatchItem(null, result.TopicPartitionOffset);
        }

        var headers = KafkaHeadersMapper.ToMessageHeaders(result.Message?.Headers);
        EventMeta? eventMeta = _metadataAccessor.GetMeta(envelope);

        string? eventId = BuildEventId(eventMeta, result);
        TracingContext tracing = BuildTracingContext(headers, eventMeta);

        var message = new KafkaConsumedMessage<T>
        {
            EventId = eventId,
            Event = envelope,
            Position = new KafkaMessagePosition(
                new KafkaTopicName(result.Topic),
                result.Partition.Value,
                ToIntOffset(result.Offset.Value)),
            Timestamp = result.Message?.Timestamp.UtcDateTime ?? DateTime.UtcNow,
            Key = new KafkaMessageKey(result.Message?.Key),
            Headers = headers,
            TracingContext = tracing,
            Payload = payload,
        };

        return new BatchItem(message, result.TopicPartitionOffset);
    }

    private async Task PublishDeserializationErrorAsync(
        ConsumeResult<string, byte[]> result,
        byte[] payload,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Failed to deserialize message on {Topic} at {Partition}:{Offset}.",
            result.Topic,
            result.Partition.Value,
            result.Offset.Value);

        if (!_deadLetterQueueOptions.Enabled)
            return;

        var headers = KafkaHeadersMapper.ToMessageHeaders(result.Message?.Headers);
        headers.WithUtf8("x-deserialization-error", exception.Message);

        await _deadLetterQueueProducer.PublishAsync(
            new KafkaTopicName(result.Topic + _deadLetterQueueOptions.TopicSuffix),
            new KafkaMessageKey(result.Message?.Key),
            payload,
            headers,
            new KafkaMessagePosition(
                new KafkaTopicName(result.Topic),
                result.Partition.Value,
                ToIntOffset(result.Offset.Value)),
            exception.ToString(),
            cancellationToken);
    }

    private string? BuildEventId(EventMeta? eventMeta, ConsumeResult<string, byte[]> result)
    {
        if (!string.IsNullOrWhiteSpace(eventMeta?.EventId))
            return eventMeta.EventId;

        return $"{result.Topic}:{result.Partition.Value}:{result.Offset.Value}";
    }

    private TracingContext BuildTracingContext(MessageHeaders headers, EventMeta? eventMeta)
    {
        TracingContext tracingFromHeaders = _tracingContextAccessor.Extract(new MessageHeadersCarrier(headers));

        if (eventMeta is null)
            return tracingFromHeaders;

        string? traceId = string.IsNullOrWhiteSpace(eventMeta.TraceId)
            ? tracingFromHeaders.TraceId
            : eventMeta.TraceId;
        string? spanId = string.IsNullOrWhiteSpace(eventMeta.SpanId)
            ? tracingFromHeaders.SpanId
            : eventMeta.SpanId;

        return new TracingContext(traceId, spanId, tracingFromHeaders.Sampled);
    }

    private sealed record BatchItem(KafkaConsumedMessage<T>? Message, TopicPartitionOffset Offset);

    private sealed record CommitRequest(
        IReadOnlyList<TopicPartitionOffset> Offsets,
        TaskCompletionSource<bool> Completion);
}
