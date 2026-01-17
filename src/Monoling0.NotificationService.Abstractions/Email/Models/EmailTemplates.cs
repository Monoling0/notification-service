namespace Monoling0.NotificationService.Email.Models;

public static class EmailTemplates
{
    public static string Welcome { get; } = "welcome";

    public static string Subscribed { get; } = "subscribed";

    public static string CoursePublished { get; } = "course_published";

    public static string CourseUpdated { get; } = "course_updated";

    public static string CourseCompleted { get; } = "course_completed";

    public static string StreakMilestone { get; } = "streak_milestone";

    public static string StreakBroken { get; } = "streak_broken";

    public static string LeaderboardPrize { get; } = "leaderboard_prize";

    public static string WeeklyReport { get; } = "weekly_report";
}
