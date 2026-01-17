using Monoling0.NotificationService.Persistence.Transactions;
using Npgsql;
using System.Data.Common;

namespace Monoling0.NotificationService.Persistence.Database;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;
    private bool _completed;

    public UnitOfWork(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
        _completed = false;
    }

    public DbConnection Connection => _connection;

    public DbTransaction Transaction => _transaction;

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (_completed)
            return;

        await _transaction.CommitAsync(cancellationToken);

        _completed = true;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (_completed)
            return;

        await _transaction.RollbackAsync(cancellationToken);

        _completed = true;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!_completed)
            {
                await _transaction.RollbackAsync(CancellationToken.None);
                _completed = true;
            }
        }
        finally
        {
            await _transaction.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
