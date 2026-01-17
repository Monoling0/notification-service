using Course;
using Monoling0.NotificationService.Application.UseCases.Common;
using Monoling0.NotificationService.Email.Models;
using Monoling0.NotificationService.Persistence.Models.Cache;
using Monoling0.NotificationService.Persistence.Repositories;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeCourseTopic.Handlers;

public sealed class CoursePublishedHandler
{
    private readonly ICoursesCacheRepository _coursesCacheRepository;
    private readonly EmailNotificationService _emailNotificationService;

    public CoursePublishedHandler(
        ICoursesCacheRepository coursesCacheRepository,
        EmailNotificationService emailNotificationService)
    {
        _coursesCacheRepository = coursesCacheRepository;
        _emailNotificationService = emailNotificationService;
    }

    public async Task HandleAsync(CoursePublishedEvent coursePublishedEvent, CancellationToken cancellationToken)
    {
        var publishedAt = coursePublishedEvent.PublishedAt.ToDateTime();

        string? description = coursePublishedEvent.HasDescription ? coursePublishedEvent.Description : null;
        string? cefrLevel = coursePublishedEvent.HasCefrLevel ? coursePublishedEvent.CefrLevel : null;
        string? language = coursePublishedEvent.HasLanguage ? coursePublishedEvent.Language : null;

        var course = new CourseCacheItem(
            coursePublishedEvent.CourseId,
            coursePublishedEvent.Title,
            description,
            cefrLevel,
            language,
            publishedAt,
            publishedAt);

        await _coursesCacheRepository.UpsertAsync(course, cancellationToken);

        long? publishedByUserId = coursePublishedEvent.HasPublishedByUserId
            ? coursePublishedEvent.PublishedByUserId
            : null;

        var model = new
        {
            coursePublishedEvent.CourseId,
            coursePublishedEvent.Title,
            Description = description,
            CefrLevel = cefrLevel,
            Language = language,
            PublishedAt = publishedAt,
            PublishedByUserId = publishedByUserId,
        };

        await _emailNotificationService.SendToAllUsersAsync(
            EmailTemplates.CoursePublished,
            model,
            cancellationToken);
    }
}
