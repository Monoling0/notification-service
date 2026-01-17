namespace Monoling0.NotificationService.Messaging.Kafka.Options;

public sealed class KafkaConsumerOptions
{
    public string BootstrapServers { get; init; } = string.Empty;

    public string GroupId { get; init; } = string.Empty;

    public bool EnableAutoCommit { get; init; } = false;

    public bool EnableAutoOffsetStore { get; init; } = false;

    public string AutoOffsetReset { get; init; } = "Earliest";

    public string ClientId { get; init; } = "notification-service";
}
