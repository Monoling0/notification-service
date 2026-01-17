namespace Monoling0.NotificationService.Persistence.Models.Cache;

public record CourseCacheItem(
    long CourseId,
    string Title,
    string? Description,
    string? CefrLevel,
    string? Language,
    DateTime? PublishedAt,
    DateTime? UpdatedAt);
