namespace Monoling0.NotificationService.Messaging.Kafka.Options;

public sealed class KafkaDlqOptions
{
    public bool Enabled { get; init; } = true;

    public string TopicSuffix { get; init; } = ".dlq";
}
