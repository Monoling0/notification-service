using Feed;
using Monoling0.NotificationService.Application.UseCases.Common;
using Monoling0.NotificationService.Email.Models;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeFeedTopic.Handlers;

public sealed class StreakMilestoneHandler
{
    private readonly EmailNotificationService _emailNotificationService;

    public StreakMilestoneHandler(EmailNotificationService emailNotificationService)
    {
        _emailNotificationService = emailNotificationService;
    }

    public async Task HandleAsync(StreakMilestoneEvent streakMilestoneEvent, CancellationToken cancellationToken)
    {
        var achievedAt = streakMilestoneEvent.AchievedAt.ToDateTime();

        var model = new
        {
            streakMilestoneEvent.UserId,
            streakMilestoneEvent.StreakDays,
            AchievedAt = achievedAt,
        };

        await _emailNotificationService.SendToUserAsync(
            EmailTemplates.StreakMilestone,
            model,
            streakMilestoneEvent.UserId,
            cancellationToken);

        await _emailNotificationService.SendToFollowersAsync(
            streakMilestoneEvent.UserId,
            EmailTemplates.StreakMilestone,
            model,
            cancellationToken);
    }
}
