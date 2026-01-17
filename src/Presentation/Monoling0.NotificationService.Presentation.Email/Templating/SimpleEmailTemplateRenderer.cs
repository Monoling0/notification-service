using Monoling0.NotificationService.Email;
using Monoling0.NotificationService.Email.Models;
using Monoling0.NotificationService.Serialization;
using System.Text;

namespace Monoling0.NotificationService.Presentation.Email.Templating;

public sealed class SimpleEmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly IJsonSerializer _jsonSerializer;

    public SimpleEmailTemplateRenderer(IJsonSerializer jsonSerializer)
    {
        _jsonSerializer = jsonSerializer;
    }

    public (string Subject, string HtmlBody, string? TextBody) Render(string kind, object model)
    {
        string subject = kind switch
        {
            _ when kind == EmailTemplates.Welcome => "Welcome to Monolingo",
            _ when kind == EmailTemplates.Subscribed => "Subscription activated",
            _ when kind == EmailTemplates.CoursePublished => "New course published",
            _ when kind == EmailTemplates.CourseUpdated => "Course updated",
            _ when kind == EmailTemplates.CourseCompleted => "Course completed",
            _ when kind == EmailTemplates.StreakMilestone => "Streak milestone",
            _ when kind == EmailTemplates.StreakBroken => "Streak broken",
            _ when kind == EmailTemplates.LeaderboardPrize => "Leaderboard prize",
            _ when kind == EmailTemplates.WeeklyReport => "Weekly report",
            _ => $"Notification: {kind}",
        };

        string payload = _jsonSerializer.Serialize(model);
        string html = new StringBuilder()
            .Append("<html><body>")
            .Append("<h2>").Append(subject).Append("</h2>")
            .Append("<pre style=\"font-family: monospace;\">")
            .Append(payload)
            .Append("</pre>")
            .Append("</body></html>")
            .ToString();
        string text = $"{subject}{Environment.NewLine}{Environment.NewLine}{payload}";

        return (subject, html, text);
    }
}
