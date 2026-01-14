using Monoling0.NotificationService.Messaging.Headers;
using Monoling0.NotificationService.Messaging.Kafka.Models;

namespace Monoling0.NotificationService.Messaging.Kafka;

public interface IKafkaProducer
{
    Task ProduceAsync<T>(
        KafkaTopicName topic,
        KafkaMessageKey key,
        T value,
        MessageHeaders? headers,
        CancellationToken cancellationToken);

    Task ProduceManyAsync<T>(
        KafkaTopicName topic,
        IReadOnlyCollection<(KafkaMessageKey Key, T Value, MessageHeaders? Headers)> messages,
        CancellationToken cancellationToken);
}
