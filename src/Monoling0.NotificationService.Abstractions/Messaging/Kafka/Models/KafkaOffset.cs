namespace Monoling0.NotificationService.Messaging.Kafka.Models;

public readonly record struct KafkaOffset(int Partition, int Offset);
