using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Monoling0.NotificationService.Email;
using Monoling0.NotificationService.Email.Models;
using Monoling0.NotificationService.Email.Options;

namespace Monoling0.NotificationService.Presentation.Email.Mail;

public sealed class MailKitEmailSender : IEmailSender
{
    private readonly IOptions<EmailSenderOptions> _options;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(IOptions<EmailSenderOptions> options, ILogger<MailKitEmailSender> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        MimeMessage mimeMessage = BuildMessage(message);

        try
        {
            using var client = new SmtpClient();

            if (_options.Value.AllowInvalidCertificates)
#pragma warning disable CA5359
                client.ServerCertificateValidationCallback = (_, _, _, _) => true;
#pragma warning restore CA5359

            client.Timeout = _options.Value.TimeoutMs;

            SecureSocketOptions socketOptions = _options.Value.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : _options.Value.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

            await client.ConnectAsync(_options.Value.Host, _options.Value.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.Value.Username))
                await client.AuthenticateAsync(_options.Value.Username, _options.Value.Password, cancellationToken);

            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return new EmailSendResult(true, mimeMessage.MessageId, null);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to send email.");
            return new EmailSendResult(false, null, exception.Message);
        }
    }

    private MimeMessage BuildMessage(EmailMessage message)
    {
        var mimeMessage = new MimeMessage();

        string senderName = _options.Value.SenderName ?? string.Empty;
        mimeMessage.From.Add(new MailboxAddress(senderName, _options.Value.SenderEmail));

        mimeMessage.To.Add(MailboxAddress.Parse(message.Receiver.Value));

        mimeMessage.Subject = message.Subject;

        string body = message.Body ?? string.Empty;
        var builder = new BodyBuilder
        {
            HtmlBody = body,
            TextBody = body,
        };

        mimeMessage.Body = builder.ToMessageBody();

        return mimeMessage;
    }
}
