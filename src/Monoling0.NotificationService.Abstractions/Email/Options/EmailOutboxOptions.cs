namespace Monoling0.NotificationService.Email.Options;

public sealed class EmailOutboxOptions
{
    public int BatchSize { get; init; } = 100;

    public int MaxAttempts { get; init; } = 10;

    public int MaxParallelism { get; init; } = 8;

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(5);

    public TimeSpan BaseBackoff { get; init; } = TimeSpan.FromSeconds(5);

    public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromSeconds(5);
}
