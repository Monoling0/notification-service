namespace Monoling0.NotificationService.Persistence.Repositories;

public interface IFollowersCacheRepository
{
    Task AddAsync(long followerId, long followeeId, DateTime occurredAt, CancellationToken cancellationToken);

    Task RemoveAsync(long followerId, long followeeId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<long>> GetFollowersAsync(long followeeId, CancellationToken cancellationToken);
}
