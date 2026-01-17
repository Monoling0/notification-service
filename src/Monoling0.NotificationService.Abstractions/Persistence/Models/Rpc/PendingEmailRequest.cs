namespace Monoling0.NotificationService.Persistence.Models.Rpc;

public class PendingEmailRequest
{
    public string CorrelationId { get; init; } = string.Empty;

    public long UserId { get; init; }

    public string Purpose { get; init; } = string.Empty;

    public string Payload { get; init; } = "{}";

    public PendingEmailRequestStatus Status { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime ExpiresAt { get; init; }

    public DateTime? CompletedAt { get; init; }

    public string? ResolvedEmail { get; init; }

    public string? Error { get; init; }
}
