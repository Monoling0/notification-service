using Monoling0.NotificationService.Email;
using Monoling0.NotificationService.Persistence.Models.Outbox;

namespace Monoling0.NotificationService.Application.Formatting;

public sealed class EmailComposer
{
    private readonly IEmailTemplateRenderer _templateRenderer;

    public EmailComposer(IEmailTemplateRenderer templateRenderer)
    {
        _templateRenderer = templateRenderer;
    }

    public EmailOutboxPayload Compose(string kind, object model)
    {
        (string subject, string htmlBody, string? textBody) = _templateRenderer.Render(kind, model);

        string body = string.IsNullOrWhiteSpace(textBody) ? htmlBody : textBody;

        return new EmailOutboxPayload(kind, subject, body);
    }

    public EmailOutboxEntry CreateOutboxEntry(
        EmailOutboxPayload payload,
        long recipientUserId,
        string toEmail,
        DateTime createdAtUtc)
    {
        return new EmailOutboxEntry
        {
            Kind = payload.Kind,
            RecipientUserId = recipientUserId,
            ToEmail = toEmail,
            Subject = payload.Subject,
            Body = payload.Body,
            Status = EmailOutboxStatus.Pending,
            AttemptsCount = 0,
            CreatedAt = createdAtUtc,
            NextAttemptAt = createdAtUtc,
        };
    }
}
