using Monoling0.NotificationService.Persistence.Models.Cache;

namespace Monoling0.NotificationService.Persistence.Repositories;

public interface ICoursesCacheRepository
{
    Task UpsertAsync(CourseCacheItem item, CancellationToken cancellationToken);

    Task<CourseCacheItem?> FindAsync(long courseId, CancellationToken cancellationToken);
}
