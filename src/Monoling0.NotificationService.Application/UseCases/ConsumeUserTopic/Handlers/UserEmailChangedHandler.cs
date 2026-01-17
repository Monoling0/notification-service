using Monoling0.NotificationService.Persistence.Repositories;
using User;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeUserTopic.Handlers;

public sealed class UserEmailChangedHandler
{
    private readonly IUserEmailCacheRepository _userEmailCacheRepository;

    public UserEmailChangedHandler(IUserEmailCacheRepository userEmailCacheRepository)
    {
        _userEmailCacheRepository = userEmailCacheRepository;
    }

    public async Task HandleAsync(UserEmailChangedEvent userEmailChangedEvent, CancellationToken cancellationToken)
    {
        var changedAt = userEmailChangedEvent.ChangedAt.ToDateTime();

        await _userEmailCacheRepository.UpsertAsync(
            userEmailChangedEvent.UserId,
            userEmailChangedEvent.NewEmail,
            changedAt,
            cancellationToken);
    }
}
