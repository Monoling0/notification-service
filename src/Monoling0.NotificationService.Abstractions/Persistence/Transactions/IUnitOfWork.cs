using System.Data.Common;

namespace Monoling0.NotificationService.Persistence.Transactions;

public interface IUnitOfWork : IAsyncDisposable
{
    DbConnection Connection { get; }

    DbTransaction Transaction { get; }

    Task CommitAsync(CancellationToken cancellationToken);

    Task RollbackAsync(CancellationToken cancellationToken);
}
