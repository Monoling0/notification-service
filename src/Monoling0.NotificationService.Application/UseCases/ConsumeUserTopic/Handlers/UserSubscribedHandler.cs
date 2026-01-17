using Monoling0.NotificationService.Application.UseCases.Common;
using Monoling0.NotificationService.Email.Models;
using User;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeUserTopic.Handlers;

public sealed class UserSubscribedHandler
{
    private readonly EmailNotificationService _emailNotificationService;

    public UserSubscribedHandler(EmailNotificationService emailNotificationService)
    {
        _emailNotificationService = emailNotificationService;
    }

    public async Task HandleAsync(UserSubscribedEvent userSubscribedEvent, CancellationToken cancellationToken)
    {
        var subscribedAt = userSubscribedEvent.SubscribedAt.ToDateTime();
        var subscriptionUntil = userSubscribedEvent.SubscriptionUntil?.ToDateTime();

        var model = new
        {
            userSubscribedEvent.UserId,
            userSubscribedEvent.PlanId,
            SubscribedAt = subscribedAt,
            SubscriptionUntil = subscriptionUntil,
        };

        await _emailNotificationService.SendToUserAsync(
            EmailTemplates.Subscribed,
            model,
            userSubscribedEvent.UserId,
            cancellationToken);
    }
}
