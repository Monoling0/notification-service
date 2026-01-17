using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monoling0.NotificationService.Email;
using Monoling0.NotificationService.Email.Models;
using Monoling0.NotificationService.Email.Options;
using Monoling0.NotificationService.Persistence.Models.Outbox;
using Monoling0.NotificationService.Persistence.Repositories;
using Monoling0.NotificationService.Presentation.Email.RateLimiting;
using Monoling0.NotificationService.RateLimiting;

namespace Monoling0.NotificationService.Presentation.Email.Services;

public sealed class EmailOutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<EmailOutboxProcessor> _logger;
    private readonly IOptions<EmailOutboxOptions> _options;

    public EmailOutboxProcessor(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<EmailOutboxProcessor> logger,
        IOptions<EmailOutboxOptions> options)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.Value.PollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Email outbox processing failed.");
            }
        }
    }

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        EmailOutboxOptions optionsValue = _options.Value;

        using IServiceScope scope = _serviceScopeFactory.CreateScope();

        IEmailOutboxRepository outbox = scope.ServiceProvider.GetRequiredService<IEmailOutboxRepository>();
        IEmailSender sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        IRateLimiter limiter = scope.ServiceProvider.GetService<IRateLimiter>() ?? RateLimiter.Instance;

        IReadOnlyCollection<EmailOutboxEntry> batch =
            await outbox.DequeueAsync(_options.Value.BatchSize, cancellationToken);

        if (batch.Count == 0)
            return;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = optionsValue.MaxParallelism,
            CancellationToken = cancellationToken,
        };

        await Parallel.ForEachAsync(
            batch,
            parallelOptions,
            async (outboxEntry, token) =>
            {
                await limiter.WaitAsync(1, token);

                var message = new EmailMessage
                {
                    Receiver = new EmailAddress(outboxEntry.ToEmail),
                    Subject = outboxEntry.Subject,
                    Body = outboxEntry.Body,
                };

                EmailSendResult result = await sender.SendEmailAsync(message, token);

                if (result.Success)
                {
                    await outbox.MarkAsSentAsync(outboxEntry.OutboxId, token);
                    return;
                }

                if (outboxEntry.AttemptsCount >= optionsValue.MaxAttempts)
                {
                    await outbox.MarkAsFailedAsync(outboxEntry.OutboxId, result.Error ?? "unknown_error", token);
                    return;
                }

                DateTime nextAttemptAt = CalculateNextAttemptTime(outboxEntry.AttemptsCount, optionsValue);
                await outbox.RescheduleAsync(outboxEntry.OutboxId, nextAttemptAt, result.Error, token);
            });
    }

    private DateTime CalculateNextAttemptTime(int attemptsCount, EmailOutboxOptions options)
    {
        int attempt = Math.Max(1, attemptsCount);
        double backoffSeconds = options.BaseBackoff.TotalSeconds * Math.Pow(2, attempt - 1);
        double clampedSeconds = Math.Min(backoffSeconds, options.MaxBackoff.TotalSeconds);
        var delay = TimeSpan.FromSeconds(clampedSeconds);

        return DateTime.UtcNow.Add(delay);
    }
}
