using Monoling0.NotificationService.Persistence.Models.Inbox;

namespace Monoling0.NotificationService.Persistence.Repositories;

public interface IInboxRepository
{
    Task<InboxDecision> TryAcquireAsync(
        string eventId, InboxEventPosition position, CancellationToken cancellationToken);

    Task MarkAsProcessedAsync(string eventId, CancellationToken cancellationToken);

    Task MarkAsFailedAsync(string eventId, string errorMessage, CancellationToken cancellationToken);
}
