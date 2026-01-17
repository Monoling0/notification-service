using Confluent.Kafka;
using Monoling0.NotificationService.Messaging.Kafka.Options;

namespace Monoling0.NotificationService.Presentation.Kafka.Configuration;

internal static class KafkaConfigBuilder
{
    public static ConsumerConfig BuildConsumer(KafkaConsumerOptions options, KafkaClientOptions clientOptions)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = options.GroupId,
            EnableAutoCommit = options.EnableAutoCommit,
            EnableAutoOffsetStore = options.EnableAutoOffsetStore,
            AutoOffsetReset = ParseAutoOffsetReset(options.AutoOffsetReset),
            ClientId = options.ClientId,
        };

        ApplyClientOptions(config, clientOptions);

        return config;
    }

    public static ProducerConfig BuildProducer(KafkaProducerOptions options, KafkaClientOptions clientOptions)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
            ClientId = options.ClientId,
        };

        if (options.MessageTimeoutMs.HasValue)
            config.MessageTimeoutMs = options.MessageTimeoutMs.Value;

        ApplyClientOptions(config, clientOptions);

        return config;
    }

    private static void ApplyClientOptions(ClientConfig config, KafkaClientOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SecurityProtocol) &&
            Enum.TryParse(options.SecurityProtocol, true, out SecurityProtocol protocol))
        {
            config.SecurityProtocol = protocol;
        }

        if (!string.IsNullOrWhiteSpace(options.SaslMechanism) &&
            Enum.TryParse(options.SaslMechanism, true, out SaslMechanism mechanism))
        {
            config.SaslMechanism = mechanism;
        }

        if (!string.IsNullOrWhiteSpace(options.SaslUsername))
            config.SaslUsername = options.SaslUsername;

        if (!string.IsNullOrWhiteSpace(options.SaslPassword))
            config.SaslPassword = options.SaslPassword;

        if (!string.IsNullOrWhiteSpace(options.SslCaLocation))
            config.SslCaLocation = options.SslCaLocation;

        config.EnableSslCertificateVerification = options.EnableSslCertificateVerification;

        foreach (KeyValuePair<string, string> item in options.AdditionalConfig)
            config.Set(item.Key, item.Value);
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string value)
    {
        return Enum.TryParse(value, true, out AutoOffsetReset parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
    }
}
