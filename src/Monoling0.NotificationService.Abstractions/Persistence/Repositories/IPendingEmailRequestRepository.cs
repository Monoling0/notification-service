using Monoling0.NotificationService.Persistence.Models.Rpc;

namespace Monoling0.NotificationService.Persistence.Repositories;

public interface IPendingEmailRequestRepository
{
    Task CreateAsync(PendingEmailRequest request, CancellationToken cancellationToken);

    Task<PendingEmailRequest?> FindAsync(string correlationId, CancellationToken cancellationToken);

    Task MarkCompletedAsync(
        string correlationId, string email, DateTime occurredAt, CancellationToken cancellationToken);

    Task MarkFailedAsync(
        string correlationId, string errorMessage, DateTime occurredAt, CancellationToken cancellationToken);
}
