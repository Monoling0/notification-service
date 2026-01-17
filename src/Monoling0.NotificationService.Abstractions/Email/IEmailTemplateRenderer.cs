namespace Monoling0.NotificationService.Email;

public interface IEmailTemplateRenderer
{
    (string Subject, string HtmlBody, string? TextBody) Render(string kind, object model);
}
