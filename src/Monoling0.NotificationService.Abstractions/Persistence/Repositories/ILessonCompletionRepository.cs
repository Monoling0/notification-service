using Monoling0.NotificationService.Persistence.Models.Progress;

namespace Monoling0.NotificationService.Persistence.Repositories;

public interface ILessonCompletionRepository
{
    Task UpsertAsync(LessonCompletion completion, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<long>> GetUsersByLessonIdsAsync(
        long courseId,
        IReadOnlyCollection<long> lessonIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<long>> GetUsersByCourseAsync(
        long courseId,
        CancellationToken cancellationToken);
}
