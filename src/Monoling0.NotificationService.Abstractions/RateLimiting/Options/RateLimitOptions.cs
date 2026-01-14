namespace Monoling0.NotificationService.RateLimiting.Options;

public sealed class RateLimitOptions
{
    public int Limit { get; init; } = 50;

    public TimeSpan Period { get; init; } = TimeSpan.FromMinutes(1);

    public int QueueLimit { get; init; } = 100;
}
