using Monoling0.NotificationService.Email.Models;

namespace Monoling0.NotificationService.Email;

public interface IEmailSender
{
    Task<EmailSendResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken);
}
