namespace Monoling0.NotificationService.Email.Models;

public sealed record EmailSendResult(bool Success, string? ProviderMessageId, string? Error);
