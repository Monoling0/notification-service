using Feed;
using Monoling0.NotificationService.Application.UseCases.Common;
using Monoling0.NotificationService.Email.Models;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeFeedTopic.Handlers;

public sealed class WeeklyReportHandler
{
    private readonly EmailNotificationService _emailNotificationService;

    public WeeklyReportHandler(EmailNotificationService emailNotificationService)
    {
        _emailNotificationService = emailNotificationService;
    }

    public async Task HandleAsync(WeeklyReportEvent weeklyReportEvent, CancellationToken cancellationToken)
    {
        var weekStart = weeklyReportEvent.WeekStart.ToDateTime();
        var weekEnd = weeklyReportEvent.WeekEnd.ToDateTime();
        var generatedAt = weeklyReportEvent.GeneratedAt.ToDateTime();
        int? newFollowersCount = weeklyReportEvent.HasNewFollowersCount
            ? weeklyReportEvent.NewFollowersCount
            : null;
        int? currentStreakDays = weeklyReportEvent.HasCurrentStreakDays
            ? weeklyReportEvent.CurrentStreakDays
            : null;

        var model = new
        {
            weeklyReportEvent.UserId,
            weeklyReportEvent.XpGained,
            weeklyReportEvent.LessonsCompleted,
            NewFollowersCount = newFollowersCount,
            CurrentStreakDays = currentStreakDays,
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            GeneratedAt = generatedAt,
        };

        await _emailNotificationService.SendToUserAsync(
            EmailTemplates.WeeklyReport,
            model,
            weeklyReportEvent.UserId,
            cancellationToken);
    }
}
