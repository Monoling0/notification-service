namespace Monoling0.NotificationService.RateLimiting;

public interface IRateLimiter
{
    ValueTask WaitAsync(int count, CancellationToken cancellationToken);
}
