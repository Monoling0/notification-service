using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Monoling0.NotificationService.Messaging.Headers;
using Monoling0.NotificationService.Messaging.Kafka;
using Monoling0.NotificationService.Messaging.Kafka.Models;
using Monoling0.NotificationService.Messaging.Kafka.Options;
using Monoling0.NotificationService.Presentation.Kafka.Configuration;
using Monoling0.NotificationService.Presentation.Kafka.Headers;
using System.Globalization;
using System.Text;

namespace Monoling0.NotificationService.Presentation.Kafka.Producers;

public sealed class ConfluentKafkaDlqProducer : IKafkaDlqProducer, IAsyncDisposable
{
    private readonly IProducer<string?, byte[]> _producer;

    public ConfluentKafkaDlqProducer(
        IOptions<KafkaProducerOptions> producerOptions,
        IOptions<KafkaClientOptions> clientOptions)
    {
        ProducerConfig config = KafkaConfigBuilder.BuildProducer(producerOptions.Value, clientOptions.Value);
        _producer = new ProducerBuilder<string?, byte[]>(config).Build();
    }

    public Task PublishAsync(
        KafkaTopicName topic,
        KafkaMessageKey key,
        byte[] data,
        MessageHeaders headers,
        KafkaMessagePosition position,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        Confluent.Kafka.Headers kafkaHeaders = KafkaHeadersMapper.ToKafkaHeaders(headers);
        kafkaHeaders.Add("x-original-topic", Encoding.UTF8.GetBytes(position.Topic.Value));
        kafkaHeaders.Add(
            "x-original-partition",
            Encoding.UTF8.GetBytes(position.Partition.ToString(CultureInfo.InvariantCulture)));
        kafkaHeaders.Add(
            "x-original-offset",
            Encoding.UTF8.GetBytes(position.Offset.ToString(CultureInfo.InvariantCulture)));
        kafkaHeaders.Add("x-error", Encoding.UTF8.GetBytes(errorMessage));

        var message = new Message<string?, byte[]>
        {
            Key = key.Value,
            Value = data,
            Headers = kafkaHeaders,
        };

        return _producer.ProduceAsync(topic.Value, message, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
        return ValueTask.CompletedTask;
    }
}
