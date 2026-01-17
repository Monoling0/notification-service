namespace Monoling0.NotificationService.Persistence.Models.Progress;

public sealed record LessonCompletion(
    long UserId,
    long CourseId,
    long LessonId,
    DateTime CompletedAt);
