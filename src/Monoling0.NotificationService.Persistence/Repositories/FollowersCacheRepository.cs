using Monoling0.NotificationService.Persistence.Database;
using Monoling0.NotificationService.Persistence.Extensions;
using Npgsql;

namespace Monoling0.NotificationService.Persistence.Repositories;

public class FollowersCacheRepository : IFollowersCacheRepository
{
    private readonly NotificationDatabaseDataSource _notificationDatabaseDataSource;

    public FollowersCacheRepository(NotificationDatabaseDataSource notificationDatabaseDataSource)
    {
        _notificationDatabaseDataSource = notificationDatabaseDataSource;
    }

    public async Task AddAsync(long followerId, long followeeId, DateTime occuredAt, CancellationToken cancellationToken)
    {
        const string sql = """
                           insert into notification.followers_cache(followee_id, follower_id, updated_at)
                           values (@followee_id, @follower_id, @updated_at)
                           on conflict (followee_id, follower_id)
                           do update set updated_at = excluded.updated_at;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command =>
            {
                command.AddParameter("followee_id", followeeId);
                command.AddParameter("follower_id", followerId);
                command.AddParameter("updated_at", occuredAt);
            });

        await command.AsNonQueryAsync(cancellationToken);
    }

    public async Task RemoveAsync(long followerId, long followeeId, CancellationToken cancellationToken)
    {
        const string sql = """
                           delete from notification.followers_cache
                           where followee_id = @followee_id and follower_id = @follower_id;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command =>
            {
                command.AddParameter("followee_id", followeeId);
                command.AddParameter("follower_id", followerId);
            });

        await command.AsNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<long>> GetFollowersAsync(long followeeId, CancellationToken cancellationToken)
    {
        const string sql = """
                           select follower_id
                           from notification.followers_cache
                           where followee_id = @followee_id
                           order by follower_id asc;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command => command.AddParameter("followee_id", followeeId));

        return await command.ReadMany(r => r.GetInt64(r.GetOrdinal("follower_id")), cancellationToken);
    }
}
