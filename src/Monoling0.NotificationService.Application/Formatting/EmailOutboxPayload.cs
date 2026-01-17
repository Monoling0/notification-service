namespace Monoling0.NotificationService.Application.Formatting;

public sealed record EmailOutboxPayload(
    string Kind,
    string Subject,
    string Body);
