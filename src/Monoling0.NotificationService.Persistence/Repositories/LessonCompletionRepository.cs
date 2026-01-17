using Monoling0.NotificationService.Persistence.Database;
using Monoling0.NotificationService.Persistence.Extensions;
using Monoling0.NotificationService.Persistence.Models.Progress;
using Npgsql;
using NpgsqlTypes;

namespace Monoling0.NotificationService.Persistence.Repositories;

public sealed class LessonCompletionRepository : ILessonCompletionRepository
{
    private readonly NotificationDatabaseDataSource _notificationDatabaseDataSource;

    public LessonCompletionRepository(NotificationDatabaseDataSource notificationDatabaseDataSource)
    {
        _notificationDatabaseDataSource = notificationDatabaseDataSource;
    }

    public async Task UpsertAsync(LessonCompletion completion, CancellationToken cancellationToken)
    {
        const string sql = """
                           insert into notification.lesson_completions(user_id, course_id, lesson_id, completed_at)
                           values (@user_id, @course_id, @lesson_id, @completed_at)
                           on conflict (user_id, course_id, lesson_id)
                           do update set completed_at = excluded.completed_at;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command =>
            {
                command.AddParameter("user_id", completion.UserId);
                command.AddParameter("course_id", completion.CourseId);
                command.AddParameter("lesson_id", completion.LessonId);
                command.AddParameter("completed_at", completion.CompletedAt, NpgsqlDbType.TimestampTz);
            });

        await command.AsNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<long>> GetUsersByLessonIdsAsync(
        long courseId,
        IReadOnlyCollection<long> lessonIds,
        CancellationToken cancellationToken)
    {
        if (lessonIds.Count == 0)
            return Array.Empty<long>();

        const string sql = """
                           select distinct user_id
                           from notification.lesson_completions
                           where course_id = @course_id and lesson_id = any(@lesson_ids)
                           order by user_id asc;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command =>
            {
                command.AddParameter("course_id", courseId);
                command.AddArrayOfParameters("lesson_ids", lessonIds, NpgsqlDbType.Bigint);
            });

        return await command.ReadMany(r => r.GetInt64(r.GetOrdinal("user_id")), cancellationToken);
    }

    public async Task<IReadOnlyCollection<long>> GetUsersByCourseAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           select distinct user_id
                           from notification.lesson_completions
                           where course_id = @course_id
                           order by user_id asc;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command => command.AddParameter("course_id", courseId));

        return await command.ReadMany(r => r.GetInt64(r.GetOrdinal("user_id")), cancellationToken);
    }
}
