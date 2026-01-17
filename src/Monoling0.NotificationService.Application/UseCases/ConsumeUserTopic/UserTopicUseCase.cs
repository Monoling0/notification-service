using Microsoft.Extensions.Logging;
using Monoling0.NotificationService.Application.UseCases.ConsumeUserTopic.Handlers;
using Monoling0.NotificationService.Messaging.Kafka.Models;
using Monoling0.NotificationService.UseCases;
using User;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeUserTopic;

public sealed class UserTopicUseCase : IEventHandler<UserEventEnvelope>
{
    private readonly UserRegisteredHandler _userRegisteredHandler;
    private readonly UserSubscribedHandler _userSubscribedHandler;
    private readonly UserEmailChangedHandler _userEmailChangedHandler;
    private readonly UserFollowedHandler _userFollowedHandler;
    private readonly UserUnfollowedHandler _userUnfollowedHandler;
    private readonly UserProfileUpdatedHandler _userProfileUpdatedHandler;
    private readonly ILogger<UserTopicUseCase> _logger;

    public UserTopicUseCase(
        UserRegisteredHandler userRegisteredHandler,
        UserSubscribedHandler userSubscribedHandler,
        UserEmailChangedHandler userEmailChangedHandler,
        UserFollowedHandler userFollowedHandler,
        UserUnfollowedHandler userUnfollowedHandler,
        UserProfileUpdatedHandler userProfileUpdatedHandler,
        ILogger<UserTopicUseCase> logger)
    {
        _userRegisteredHandler = userRegisteredHandler;
        _userSubscribedHandler = userSubscribedHandler;
        _userEmailChangedHandler = userEmailChangedHandler;
        _userFollowedHandler = userFollowedHandler;
        _userUnfollowedHandler = userUnfollowedHandler;
        _userProfileUpdatedHandler = userProfileUpdatedHandler;
        _logger = logger;
    }

    public async Task HandleAsync(KafkaConsumedMessage<UserEventEnvelope> message, CancellationToken cancellationToken)
    {
        UserEventEnvelope envelope = message.Event;

        switch (envelope.EventCase)
        {
            case UserEventEnvelope.EventOneofCase.UserRegistered:
                await _userRegisteredHandler.HandleAsync(envelope.UserRegistered, cancellationToken);
                break;
            case UserEventEnvelope.EventOneofCase.UserSubscribed:
                await _userSubscribedHandler.HandleAsync(envelope.UserSubscribed, cancellationToken);
                break;
            case UserEventEnvelope.EventOneofCase.UserEmailChanged:
                await _userEmailChangedHandler.HandleAsync(envelope.UserEmailChanged, cancellationToken);
                break;
            case UserEventEnvelope.EventOneofCase.UserFollowed:
                await _userFollowedHandler.HandleAsync(envelope.UserFollowed, cancellationToken);
                break;
            case UserEventEnvelope.EventOneofCase.UserUnfollowed:
                await _userUnfollowedHandler.HandleAsync(envelope.UserUnfollowed, cancellationToken);
                break;
            case UserEventEnvelope.EventOneofCase.UserProfileUpdated:
                await _userProfileUpdatedHandler.HandleAsync(envelope.UserProfileUpdated, cancellationToken);
                break;
            case UserEventEnvelope.EventOneofCase.None:
                _logger.LogWarning("User event envelope with empty payload received.");
                break;
            default:
                _logger.LogWarning("Unhandled user event type {EventType}.", envelope.EventCase);
                break;
        }
    }
}
