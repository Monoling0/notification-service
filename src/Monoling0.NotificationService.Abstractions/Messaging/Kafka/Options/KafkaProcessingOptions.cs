namespace Monoling0.NotificationService.Messaging.Kafka.Options;

public sealed class KafkaProcessingOptions
{
    public int MaxParallelism { get; init; } = Environment.ProcessorCount;

    public bool CommitOnError { get; init; } = true;

    public bool PublishToDlqOnError { get; init; } = true;
}
