namespace Monoling0.NotificationService.Messaging.Kafka.Options;

public sealed class KafkaClientOptions
{
    public string? SecurityProtocol { get; init; }

    public string? SaslMechanism { get; init; }

    public string? SaslUsername { get; init; }

    public string? SaslPassword { get; init; }

    public string? SslCaLocation { get; init; }

    public bool EnableSslCertificateVerification { get; init; } = true;

    public Dictionary<string, string> AdditionalConfig { get; init; } = new();
}
