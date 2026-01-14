namespace Monoling0.NotificationService.Messaging.Kafka.Options;

public sealed class KafkaProducerOptions
{
    public string BootstrapServers { get; init; } = string.Empty;

    public string ClientId { get; init; } = "notification-service";

    public int? MessageTimeoutMs { get; init; }
}
