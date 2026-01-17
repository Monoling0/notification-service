using User;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeUserTopic.Handlers;

public sealed class UserProfileUpdatedHandler
{
    public Task HandleAsync(UserProfileUpdatedEvent userProfileUpdatedEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
