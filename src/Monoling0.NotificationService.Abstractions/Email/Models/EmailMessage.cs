namespace Monoling0.NotificationService.Email.Models;

public sealed class EmailMessage
{
    public required EmailAddress Receiver { get; init; }

    public required string Subject { get; init; }

    public string? Body { get; init; }
}
