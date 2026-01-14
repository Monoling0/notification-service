using Monoling0.NotificationService.Persistence.Database;
using Monoling0.NotificationService.Persistence.Extensions;
using Npgsql;
using NpgsqlTypes;

namespace Monoling0.NotificationService.Persistence.Repositories;

public class UserEmailCacheRepository : IUserEmailCacheRepository
{
    private readonly NotificationDatabaseDataSource _notificationDatabaseDataSource;

    public UserEmailCacheRepository(NotificationDatabaseDataSource notificationDatabaseDataSource)
    {
        _notificationDatabaseDataSource = notificationDatabaseDataSource;
    }

    public async Task UpsertAsync(long userId, string email, DateTime updatedAt, CancellationToken cancellationToken)
    {
        const string sql = """
                           insert into notification.users_email_cache(user_id, email, updated_at)
                           values (@user_id, @email, @updated_at)
                           on conflict (user_id)
                           do update set email = excluded.email, updated_at = excluded.updated_at;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command =>
            {
                command.AddParameter("user_id", userId);
                command.AddParameter("email", email);
                command.AddParameter("updated_at", updatedAt, NpgsqlDbType.TimestampTz);
            });

        await command.AsNonQueryAsync(cancellationToken);
    }

    public async Task<string?> TryGetEmailAsync(long userId, CancellationToken cancellationToken)
    {
        const string sql = """
                           select email
                           from notification.users_email_cache
                           where user_id = @user_id;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command => command.AddParameter("user_id", userId));

        return await command.AsScalarAsync<string?>(cancellationToken);
    }

    public async Task<Dictionary<long, string>> GetEmailsAsync(IReadOnlyCollection<long> userIds, CancellationToken cancellationToken)
    {
        const string sql = """
                           select user_id, email
                           from notification.users_email_cache
                           where user_id = any(@user_ids);
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command => command.AddArrayOfParameters("user_id", userIds, NpgsqlDbType.Bigint));

        List<(long Id, string Email)> items = await command.ReadMany(
            r =>
            {
                long id = r.GetInt64(r.GetOrdinal("user_id"));
                string email = r.GetString(r.GetOrdinal("email"));
                return (id, email);
            },
            cancellationToken);

        var result = new Dictionary<long, string>();
        foreach ((long id, string email) in items)
            result[id] = email;

        return result;
    }
}
