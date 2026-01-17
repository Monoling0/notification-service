using Feed;
using Monoling0.NotificationService.Application.UseCases.Common;
using Monoling0.NotificationService.Email.Models;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeFeedTopic.Handlers;

public sealed class LeaderboardPrizeHandler
{
    private readonly EmailNotificationService _emailNotificationService;

    public LeaderboardPrizeHandler(EmailNotificationService emailNotificationService)
    {
        _emailNotificationService = emailNotificationService;
    }

    public async Task HandleAsync(LeaderboardPrizeEvent leaderboardPrizeEvent, CancellationToken cancellationToken)
    {
        var weekStart = leaderboardPrizeEvent.WeekStart.ToDateTime();
        var weekEnd = leaderboardPrizeEvent.WeekEnd.ToDateTime();
        var createdAt = leaderboardPrizeEvent.CreatedAt.ToDateTime();

        var model = new
        {
            leaderboardPrizeEvent.UserId,
            leaderboardPrizeEvent.Position,
            leaderboardPrizeEvent.BucketId,
            leaderboardPrizeEvent.ScoreXp,
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            CreatedAt = createdAt,
        };

        await _emailNotificationService.SendToUserAsync(
            EmailTemplates.LeaderboardPrize,
            model,
            leaderboardPrizeEvent.UserId,
            cancellationToken);

        await _emailNotificationService.SendToFollowersAsync(
            leaderboardPrizeEvent.UserId,
            EmailTemplates.LeaderboardPrize,
            model,
            cancellationToken);
    }
}
