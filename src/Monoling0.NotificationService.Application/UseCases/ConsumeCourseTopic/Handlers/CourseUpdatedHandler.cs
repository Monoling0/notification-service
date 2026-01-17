using Course;
using Monoling0.NotificationService.Application.UseCases.Common;
using Monoling0.NotificationService.Email.Models;
using Monoling0.NotificationService.Persistence.Models.Cache;
using Monoling0.NotificationService.Persistence.Repositories;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeCourseTopic.Handlers;

public sealed class CourseUpdatedHandler
{
    private readonly ICoursesCacheRepository _coursesCacheRepository;
    private readonly ILessonCompletionRepository _lessonCompletionRepository;
    private readonly EmailNotificationService _emailNotificationService;

    public CourseUpdatedHandler(
        ICoursesCacheRepository coursesCacheRepository,
        ILessonCompletionRepository lessonCompletionRepository,
        EmailNotificationService emailNotificationService)
    {
        _coursesCacheRepository = coursesCacheRepository;
        _lessonCompletionRepository = lessonCompletionRepository;
        _emailNotificationService = emailNotificationService;
    }

    public async Task HandleAsync(CourseUpdatedEvent courseUpdatedEvent, CancellationToken cancellationToken)
    {
        var updatedAt = courseUpdatedEvent.UpdatedAt.ToDateTime();
        CourseCacheItem? existing = await _coursesCacheRepository
            .FindAsync(courseUpdatedEvent.CourseId, cancellationToken);

        string title = courseUpdatedEvent.HasTitle
            ? courseUpdatedEvent.Title
            : existing?.Title ?? $"Course {courseUpdatedEvent.CourseId}";
        string? description = courseUpdatedEvent.HasSummary
            ? courseUpdatedEvent.Summary
            : existing?.Description;
        long? updatedByUserId = courseUpdatedEvent.HasUpdatedByUserId
            ? courseUpdatedEvent.UpdatedByUserId
            : null;

        var updated = new CourseCacheItem(
            courseUpdatedEvent.CourseId,
            title,
            description,
            existing?.CefrLevel,
            existing?.Language,
            existing?.PublishedAt,
            updatedAt);

        await _coursesCacheRepository.UpsertAsync(updated, cancellationToken);

        if (!courseUpdatedEvent.RequireNotification)
            return;

        long[] affectedModuleIds = courseUpdatedEvent.AffectedModuleIds.ToArray();
        long[] affectedLessonIds = courseUpdatedEvent.AffectedLessonIds.ToArray();

        IReadOnlyCollection<long> userIds = affectedLessonIds.Length > 0
            ? await _lessonCompletionRepository.GetUsersByLessonIdsAsync(
                courseUpdatedEvent.CourseId,
                affectedLessonIds,
                cancellationToken)
            : await _lessonCompletionRepository.GetUsersByCourseAsync(
                courseUpdatedEvent.CourseId,
                cancellationToken);

        if (userIds.Count == 0)
            return;

        var model = new
        {
            courseUpdatedEvent.CourseId,
            Title = title,
            Summary = description,
            UpdatedAt = updatedAt,
            AffectedModuleIds = affectedModuleIds,
            AffectedLessonIds = affectedLessonIds,
            UpdatedByUserId = updatedByUserId,
        };

        await _emailNotificationService.SendToUsersAsync(
            EmailTemplates.CourseUpdated,
            model,
            userIds,
            cancellationToken);
    }
}
