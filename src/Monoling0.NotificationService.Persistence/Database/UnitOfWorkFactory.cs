using Monoling0.NotificationService.Persistence.Transactions;
using Npgsql;

namespace Monoling0.NotificationService.Persistence.Database;

public sealed class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly NotificationDatabaseDataSource _notificationDatabaseDataSource;

    public UnitOfWorkFactory(NotificationDatabaseDataSource notificationDatabaseDataSource)
    {
        _notificationDatabaseDataSource = notificationDatabaseDataSource;
    }

    public async Task<IUnitOfWork> BeginAsync(CancellationToken cancellationToken)
    {
        NpgsqlConnection connection = await _notificationDatabaseDataSource.DataSource.OpenConnectionAsync(cancellationToken);
        NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        return new UnitOfWork(connection, transaction);
    }
}
