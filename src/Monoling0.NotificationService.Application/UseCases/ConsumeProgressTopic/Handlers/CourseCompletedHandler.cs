using Monoling0.NotificationService.Application.UseCases.Common;
using Monoling0.NotificationService.Email.Models;
using Monoling0.NotificationService.Persistence.Repositories;
using Progress;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeProgressTopic.Handlers;

public sealed class CourseCompletedHandler
{
    private readonly ICoursesCacheRepository _coursesCacheRepository;
    private readonly EmailNotificationService _emailNotificationService;

    public CourseCompletedHandler(
        ICoursesCacheRepository coursesCacheRepository,
        EmailNotificationService emailNotificationService)
    {
        _coursesCacheRepository = coursesCacheRepository;
        _emailNotificationService = emailNotificationService;
    }

    public async Task HandleAsync(CourseCompletedEvent courseCompletedEvent, CancellationToken cancellationToken)
    {
        var completedAt = courseCompletedEvent.CompletedAt.ToDateTime();
        Persistence.Models.Cache.CourseCacheItem? course =
            await _coursesCacheRepository.FindAsync(courseCompletedEvent.CourseId, cancellationToken);

        var model = new
        {
            courseCompletedEvent.UserId,
            courseCompletedEvent.CourseId,
            CourseTitle = course?.Title,
            CourseDescription = course?.Description,
            CompletedAt = completedAt,
        };

        await _emailNotificationService.SendToUserAsync(
            EmailTemplates.CourseCompleted,
            model,
            courseCompletedEvent.UserId,
            cancellationToken);

        await _emailNotificationService.SendToFollowersAsync(
            courseCompletedEvent.UserId,
            EmailTemplates.CourseCompleted,
            model,
            cancellationToken);
    }
}