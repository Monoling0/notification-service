using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monoling0.NotificationService.Email;
using Monoling0.NotificationService.Email.Options;
using Monoling0.NotificationService.Presentation.Email.Mail;
using Monoling0.NotificationService.Presentation.Email.RateLimiting;
using Monoling0.NotificationService.Presentation.Email.Services;
using Monoling0.NotificationService.Presentation.Email.Templating;
using Monoling0.NotificationService.RateLimiting;
using Monoling0.NotificationService.RateLimiting.Options;

namespace Monoling0.NotificationService.Presentation.Email.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresentationEmail(this IServiceCollection services)
    {
        services.AddSingleton<IEmailSender, MailKitEmailSender>();
        services.AddSingleton<IEmailTemplateRenderer, SimpleEmailTemplateRenderer>();
        services.AddSingleton<IRateLimiter, TokenBucketRateLimiterAdapter>();
        services.AddHostedService<EmailOutboxProcessor>();

        return services;
    }

    public static IServiceCollection AddPresentationEmailOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<EmailSenderOptions>()
            .Bind(configuration.GetSection("Email:Sender"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<EmailOutboxOptions>()
            .Bind(configuration.GetSection("Email:Outbox"))
            .ValidateOnStart();

        services.AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection("Email:RateLimit"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
