namespace Monoling0.NotificationService.Persistence.Models.Outbox;

public class EmailOutboxEntry
{
    public long OutboxId { get; init; }

    public string Kind { get; init; } = string.Empty;

    public long? RecipientUserId { get; init; }

    public string ToEmail { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public EmailOutboxStatus Status { get; init; }

    public int AttemptsCount { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime NextAttemptAt { get; init; }

    public DateTime LastAttemptAt { get; init; }

    public DateTime SentAt { get; init; }

    public string? LastError { get; init; }
}
