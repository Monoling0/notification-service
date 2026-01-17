using Monoling0.NotificationService.Messaging.Headers;
using Monoling0.NotificationService.Messaging.Kafka.Models;

namespace Monoling0.NotificationService.Messaging.Kafka;

public interface IKafkaDlqProducer
{
    Task PublishAsync(
        KafkaTopicName topic,
        KafkaMessageKey key,
        byte[] data,
        MessageHeaders headers,
        KafkaMessagePosition position,
        string errorMessage,
        CancellationToken cancellationToken);
}
