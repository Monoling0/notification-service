namespace Monoling0.NotificationService.Messaging.Kafka.Models;

public sealed record KafkaMessagePosition(
    KafkaTopicName Topic,
    int Partition,
    int Offset);
