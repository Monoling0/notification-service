using Feed;
using Monoling0.NotificationService.Application.UseCases.Common;
using Monoling0.NotificationService.Email.Models;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeFeedTopic.Handlers;

public sealed class StreakBrokenHandler
{
    private readonly EmailNotificationService _emailNotificationService;

    public StreakBrokenHandler(EmailNotificationService emailNotificationService)
    {
        _emailNotificationService = emailNotificationService;
    }

    public async Task HandleAsync(StreakBrokenEvent streakBrokenEvent, CancellationToken cancellationToken)
    {
        var brokenAt = streakBrokenEvent.BrokenAt.ToDateTime();

        var model = new
        {
            streakBrokenEvent.UserId,
            streakBrokenEvent.StreakDays,
            BrokenAt = brokenAt,
        };

        await _emailNotificationService.SendToUserAsync(
            EmailTemplates.StreakBroken,
            model,
            streakBrokenEvent.UserId,
            cancellationToken);
    }
}
