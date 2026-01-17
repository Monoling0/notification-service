using Monoling0.NotificationService.Persistence.Models.Progress;
using Monoling0.NotificationService.Persistence.Repositories;
using Progress;

namespace Monoling0.NotificationService.Application.UseCases.ConsumeProgressTopic.Handlers;

public sealed class LessonCompletedHandler
{
    private readonly ILessonCompletionRepository _lessonCompletionRepository;

    public LessonCompletedHandler(ILessonCompletionRepository lessonCompletionRepository)
    {
        _lessonCompletionRepository = lessonCompletionRepository;
    }

    public async Task HandleAsync(LessonCompletedEvent lessonCompletedEvent, CancellationToken cancellationToken)
    {
        var completedAt = lessonCompletedEvent.CompletedAt.ToDateTime();
        var completion = new LessonCompletion(
            lessonCompletedEvent.UserId,
            lessonCompletedEvent.CourseId,
            lessonCompletedEvent.LessonId,
            completedAt);

        await _lessonCompletionRepository.UpsertAsync(completion, cancellationToken);
    }
}
