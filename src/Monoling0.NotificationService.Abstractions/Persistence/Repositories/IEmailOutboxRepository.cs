using Monoling0.NotificationService.Persistence.Models.Outbox;

namespace Monoling0.NotificationService.Persistence.Repositories;

public interface IEmailOutboxRepository
{
    Task<long> EnqueueAsync(EmailOutboxEntry message, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<EmailOutboxEntry>> DequeueAsync(int batchSize, CancellationToken cancellationToken);

    Task MarkAsSentAsync(long outboxMessageId, CancellationToken cancellationToken);

    Task MarkAsFailedAsync(long outboxMessageId, string errorMessage, CancellationToken cancellationToken);

    Task RescheduleAsync(
        long outboxMessageId, DateTime nextAttemptAt, string? lastError, CancellationToken cancellationToken);
}
