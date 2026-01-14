namespace Monoling0.NotificationService.Persistence.Repositories;

public interface IUserEmailCacheRepository
{
    Task UpsertAsync(long userId, string email, DateTime updatedAt, CancellationToken cancellationToken);

    Task<string?> TryGetEmailAsync(long userId, CancellationToken cancellationToken);

    Task<Dictionary<long, string>> GetEmailsAsync(
        IReadOnlyCollection<long> userIds, CancellationToken cancellationToken);
}
