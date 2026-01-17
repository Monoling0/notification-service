using Course;
using Monoling0.NotificationService.Persistence.Models.Cache;
using Monoling0.NotificationService.Persistence.Repositories;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeCourseTopic.Handlers;

public sealed class CourseUnpublishedHandler
{
    private readonly ICoursesCacheRepository _coursesCacheRepository;

    public CourseUnpublishedHandler(ICoursesCacheRepository coursesCacheRepository)
    {
        _coursesCacheRepository = coursesCacheRepository;
    }

    public async Task HandleAsync(CourseUnpublishedEvent courseUnpublishedEvent, CancellationToken cancellationToken)
    {
        var unpublishedAt = courseUnpublishedEvent.UnpublishedAt.ToDateTime();
        CourseCacheItem? existing = await _coursesCacheRepository
            .FindAsync(courseUnpublishedEvent.CourseId, cancellationToken);

        var updated = new CourseCacheItem(
            courseUnpublishedEvent.CourseId,
            existing?.Title ?? $"Course {courseUnpublishedEvent.CourseId}",
            existing?.Description,
            existing?.CefrLevel,
            existing?.Language,
            null,
            unpublishedAt);

        await _coursesCacheRepository.UpsertAsync(updated, cancellationToken);
    }
}
