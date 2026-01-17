using Confluent.Kafka;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monoling0.NotificationService.Messaging.Headers;
using Monoling0.NotificationService.Messaging.Kafka;
using Monoling0.NotificationService.Messaging.Kafka.Models;
using Monoling0.NotificationService.Messaging.Kafka.Options;
using Monoling0.NotificationService.Presentation.Kafka.Configuration;
using Monoling0.NotificationService.Presentation.Kafka.Headers;

namespace Monoling0.NotificationService.Presentation.Kafka.Producers;

public sealed class ConfluentKafkaProducer : IKafkaProducer, IAsyncDisposable
{
    private readonly IProducer<string?, byte[]> _producer;
    private readonly IServiceProvider _serviceProvider;

    public ConfluentKafkaProducer(
        IServiceProvider serviceProvider,
        IOptions<KafkaProducerOptions> producerOptions,
        IOptions<KafkaClientOptions> clientOptions)
    {
        _serviceProvider = serviceProvider;
        ProducerConfig config = KafkaConfigBuilder.BuildProducer(producerOptions.Value, clientOptions.Value);
        _producer = new ProducerBuilder<string?, byte[]>(config).Build();
    }

    public Task ProduceAsync<T>(
        KafkaTopicName topic,
        KafkaMessageKey key,
        T value,
        MessageHeaders? headers,
        CancellationToken cancellationToken) where T : class, IMessage<T>, new()
    {
        IKafkaSerializer<T> serializer = _serviceProvider.GetRequiredService<IKafkaSerializer<T>>();
        byte[] payload = serializer.Serialize(value);

        var message = new Message<string?, byte[]>
        {
            Key = key.Value,
            Value = payload,
            Headers = KafkaHeadersMapper.ToKafkaHeaders(headers),
        };

        return _producer.ProduceAsync(topic.Value, message, cancellationToken);
    }

    public async Task ProduceManyAsync<T>(
        KafkaTopicName topic,
        IReadOnlyCollection<(KafkaMessageKey Key, T Value, MessageHeaders? Headers)> messages,
        CancellationToken cancellationToken) where T : class, IMessage<T>, new()
    {
        IKafkaSerializer<T> serializer = _serviceProvider.GetRequiredService<IKafkaSerializer<T>>();
        var tasks = new List<Task>(messages.Count);

        foreach ((KafkaMessageKey key, T value, MessageHeaders? headers) in messages)
        {
            byte[] payload = serializer.Serialize(value);

            var message = new Message<string?, byte[]>
            {
                Key = key.Value,
                Value = payload,
                Headers = KafkaHeadersMapper.ToKafkaHeaders(headers),
            };

            tasks.Add(_producer.ProduceAsync(topic.Value, message, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    public ValueTask DisposeAsync()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
        return ValueTask.CompletedTask;
    }
}
