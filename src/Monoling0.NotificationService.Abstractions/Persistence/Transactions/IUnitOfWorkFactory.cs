namespace Monoling0.NotificationService.Persistence.Transactions;

public interface IUnitOfWorkFactory
{
    Task<IUnitOfWork> BeginAsync(CancellationToken cancellationToken);
}
