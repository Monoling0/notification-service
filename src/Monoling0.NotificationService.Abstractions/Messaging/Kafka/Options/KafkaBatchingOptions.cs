namespace Monoling0.NotificationService.Messaging.Kafka.Options;

public sealed class KafkaBatchingOptions
{
    public int MaxBatchSize { get; init; } = 100;

    public TimeSpan BatchTimeout { get; init; } = TimeSpan.FromSeconds(5);

    public int ChannelCapacity { get; init; } = 100;

    public bool WaitWhenChannelFull { get; init; } = true;
}
