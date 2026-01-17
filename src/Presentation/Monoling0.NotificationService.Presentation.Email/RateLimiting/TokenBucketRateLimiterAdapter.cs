using Microsoft.Extensions.Options;
using Monoling0.NotificationService.RateLimiting;
using Monoling0.NotificationService.RateLimiting.Options;
using System.Threading.RateLimiting;

namespace Monoling0.NotificationService.Presentation.Email.RateLimiting;

public sealed class TokenBucketRateLimiterAdapter : IRateLimiter, IAsyncDisposable
{
    private readonly TokenBucketRateLimiter _limiter;

    public TokenBucketRateLimiterAdapter(IOptions<RateLimitOptions> options)
    {
        RateLimitOptions value = options.Value;

        _limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = value.Limit,
            TokensPerPeriod = value.Limit,
            ReplenishmentPeriod = value.Period,
            AutoReplenishment = true,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = value.QueueLimit,
        });
    }

    public async ValueTask WaitAsync(int count, CancellationToken cancellationToken)
    {
        using RateLimitLease lease = await _limiter.AcquireAsync(count, cancellationToken);

        if (!lease.IsAcquired)
            throw new InvalidOperationException("Rate limit exceeded.");
    }

    public ValueTask DisposeAsync()
    {
        _limiter.Dispose();
        return ValueTask.CompletedTask;
    }
}
