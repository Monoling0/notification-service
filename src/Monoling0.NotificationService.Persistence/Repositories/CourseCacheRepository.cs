using Monoling0.NotificationService.Persistence.Database;
using Monoling0.NotificationService.Persistence.Extensions;
using Monoling0.NotificationService.Persistence.Models.Cache;
using Npgsql;
using NpgsqlTypes;

namespace Monoling0.NotificationService.Persistence.Repositories;

public class CourseCacheRepository : ICoursesCacheRepository
{
    private readonly NotificationDatabaseDataSource _notificationDatabaseDataSource;

    public CourseCacheRepository(NotificationDatabaseDataSource notificationDatabaseDataSource)
    {
        _notificationDatabaseDataSource = notificationDatabaseDataSource;
    }

    public async Task UpsertAsync(CourseCacheItem item, CancellationToken cancellationToken)
    {
        const string sql = """
                           insert into notification.courses_cache(course_id, title, description, cefr_level, language, published_at, updated_at)
                           values (@course_id, @title, @description, @cefr_level, @language, @published_at, @updated_at)
                           on conflict (course_id) do update set
                               title = excluded.title,
                               description = excluded.description,
                               cefr_level = excluded.cefr_level,
                               language = excluded.language,
                               published_at = excluded.published_at,
                               updated_at = excluded.updated_at;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command =>
            {
                command.AddParameter("course_id", item.CourseId);
                command.AddParameter("title", item.Title);
                command.AddParameter("description", item.Description);
                command.AddParameter("cefr_level", item.CefrLevel);
                command.AddParameter("language", item.Language);
                command.AddParameter("published_at", item.PublishedAt, NpgsqlDbType.TimestampTz);
                command.AddParameter("updated_at", item.UpdatedAt, NpgsqlDbType.TimestampTz);
            });

        await command.AsNonQueryAsync(cancellationToken);
    }

    public async Task<CourseCacheItem?> FindAsync(long courseId, CancellationToken cancellationToken)
    {
        const string sql = """
                           select course_id, title, description, cefr_level, language, published_at, updated_at
                           from notification.courses_cache
                           where course_id = @course_id;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command => command.AddParameter("course_id", courseId));

        return await command.ReadOnce(
            r =>
            {
                long id = r.GetInt64(r.GetOrdinal("course_id"));
                string title = r.GetString(r.GetOrdinal("title"));
                string? description = r.IsDBNull(r.GetOrdinal("description"))
                    ? null
                    : r.GetString(r.GetOrdinal("description"));
                string? cefrLevel = r.IsDBNull(r.GetOrdinal("cefr_level"))
                    ? null
                    : r.GetString(r.GetOrdinal("cefr_level"));
                string? language = r.IsDBNull(r.GetOrdinal("language"))
                    ? null
                    : r.GetString(r.GetOrdinal("language"));
                DateTime? publishedAt = r.IsDBNull(r.GetOrdinal("published_at"))
                    ? null
                    : r.GetFieldValue<DateTime>(r.GetOrdinal("published_at"));
                DateTime updatedAt = r.GetFieldValue<DateTime>(r.GetOrdinal("updated_at"));

                return new CourseCacheItem(id, title, description, cefrLevel, language, publishedAt, updatedAt);
            },
            cancellationToken);
    }
}
