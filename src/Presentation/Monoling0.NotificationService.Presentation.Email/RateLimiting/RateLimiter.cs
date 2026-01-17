using Monoling0.NotificationService.RateLimiting;

namespace Monoling0.NotificationService.Presentation.Email.RateLimiting;

public sealed class RateLimiter : IRateLimiter
{
    public static RateLimiter Instance { get; } = new();

    private RateLimiter() { }

    public ValueTask WaitAsync(int count, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
