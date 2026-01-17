using System.ComponentModel.DataAnnotations;

namespace Monoling0.NotificationService.Email.Options;

public class EmailSenderOptions
{
    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = 587;

    public string? Username { get; init; }

    public string? Password { get; init; }

    public bool UseSsl { get; init; } = false;

    public bool UseStartTls { get; init; } = true;

    [Range(1000, 60000)]
    public int TimeoutMs { get; init; } = 10000;

    [Required]
    public string SenderEmail { get; init; } = string.Empty;

    public string? SenderName { get; init; }

    public bool AllowInvalidCertificates { get; init; } = false;
}
