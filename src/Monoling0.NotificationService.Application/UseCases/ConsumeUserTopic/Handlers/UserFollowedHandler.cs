using Monoling0.NotificationService.Persistence.Repositories;
using User;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeUserTopic.Handlers;

public sealed class UserFollowedHandler
{
    private readonly IFollowersCacheRepository _followersCacheRepository;

    public UserFollowedHandler(IFollowersCacheRepository followersCacheRepository)
    {
        _followersCacheRepository = followersCacheRepository;
    }

    public async Task HandleAsync(UserFollowedEvent userFollowedEvent, CancellationToken cancellationToken)
    {
        var followedAt = userFollowedEvent.FollowedAt.ToDateTime();

        await _followersCacheRepository.AddAsync(
            userFollowedEvent.FollowerId,
            userFollowedEvent.FolloweeId,
            followedAt,
            cancellationToken);
    }
}
