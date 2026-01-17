using Monoling0.NotificationService.Persistence.Repositories;
using User;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeUserTopic.Handlers;

public sealed class UserUnfollowedHandler
{
    private readonly IFollowersCacheRepository _followersCacheRepository;

    public UserUnfollowedHandler(IFollowersCacheRepository followersCacheRepository)
    {
        _followersCacheRepository = followersCacheRepository;
    }

    public async Task HandleAsync(UserUnfollowedEvent userUnfollowedEvent, CancellationToken cancellationToken)
    {
        await _followersCacheRepository.RemoveAsync(
            userUnfollowedEvent.FollowerId,
            userUnfollowedEvent.FolloweeId,
            cancellationToken);
    }
}
