using Course;
using Feed;
using Microsoft.Extensions.DependencyInjection;
using Monoling0.NotificationService.Application.Formatting;
using Monoling0.NotificationService.Application.UseCases.Common;
using Monoling0.NotificationService.Application.UseCases.ConsumeCourseTopic;
using Monoling0.NotificationService.Application.UseCases.ConsumeCourseTopic.Handlers;
using Monoling0.NotificationService.Application.UseCases.ConsumeFeedTopic;
using Monoling0.NotificationService.Application.UseCases.ConsumeFeedTopic.Handlers;
using Monoling0.NotificationService.Application.UseCases.ConsumeProgressTopic;
using Monoling0.NotificationService.Application.UseCases.ConsumeProgressTopic.Handlers;
using Monoling0.NotificationService.Application.UseCases.ConsumeUserRpcTopic;
using Monoling0.NotificationService.Application.UseCases.ConsumeUserRpcTopic.Handlers;
using Monoling0.NotificationService.Application.UseCases.ConsumeUserTopic;
using Monoling0.NotificationService.Application.UseCases.ConsumeUserTopic.Handlers;
using Monoling0.NotificationService.UseCases;
using Progress;
using User;

namespace Monoling0.NotificationService.Application.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<EmailComposer>();
        services.AddScoped<EmailNotificationService>();

        services.AddScoped<UserRegisteredHandler>();
        services.AddScoped<UserSubscribedHandler>();
        services.AddScoped<UserEmailChangedHandler>();
        services.AddScoped<UserFollowedHandler>();
        services.AddScoped<UserUnfollowedHandler>();
        services.AddScoped<UserProfileUpdatedHandler>();
        services.AddScoped<IEventHandler<UserEventEnvelope>, UserTopicUseCase>();

        services.AddScoped<CoursePublishedHandler>();
        services.AddScoped<CourseUpdatedHandler>();
        services.AddScoped<CourseUnpublishedHandler>();
        services.AddScoped<IEventHandler<CourseEventEnvelope>, CourseTopicUseCase>();

        services.AddScoped<LessonCompletedHandler>();
        services.AddScoped<CourseCompletedHandler>();
        services.AddScoped<IEventHandler<ProgressEventEnvelope>, ProgressTopicUseCase>();

        services.AddScoped<StreakMilestoneHandler>();
        services.AddScoped<StreakBrokenHandler>();
        services.AddScoped<LeaderboardPrizeHandler>();
        services.AddScoped<WeeklyReportHandler>();
        services.AddScoped<IEventHandler<FeedEventEnvelope>, FeedTopicUseCase>();

        services.AddScoped<UserEmailResponseHandler>();
        services.AddScoped<UserEmailsBatchResponseHandler>();
        services.AddScoped<IEventHandler<UserRpcEventEnvelope>, UserRpcTopicUseCase>();

        return services;
    }
}
