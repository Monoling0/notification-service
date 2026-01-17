using Monoling0.NotificationService.Common;
using Monoling0.NotificationService.Persistence.Database;
using Monoling0.NotificationService.Persistence.Extensions;
using Monoling0.NotificationService.Persistence.Models.Rpc;
using Npgsql;
using NpgsqlTypes;

namespace Monoling0.NotificationService.Persistence.Repositories;

public class PendingEmailRequestRepository : IPendingEmailRequestRepository
{
    private readonly NotificationDatabaseDataSource _notificationDatabaseDataSource;

    public PendingEmailRequestRepository(NotificationDatabaseDataSource notificationDatabaseDataSource)
    {
        _notificationDatabaseDataSource = notificationDatabaseDataSource;
    }

    public async Task CreateAsync(PendingEmailRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
                           insert into notification.pending_email_requests
                               (correlation_id, user_id, purpose, payload_json, status, created_at, expires_at)
                           values
                               (@correlation_id, @user_id, @purpose, @payload_json, @status, @created_at, @expires_at)
                           on conflict (correlation_id) do nothing;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command =>
            {
                command.AddParameter("correlation_id", request.CorrelationId);
                command.AddParameter("user_id", request.UserId);
                command.AddParameter("purpose", request.Purpose);
                command.AddParameter("payload_json", request.Payload);
                command.AddParameter(
                    "status",
                    EnumDatabaseCodeConverter<PendingEmailRequestStatus>.ToDatabaseCode(request.Status));
                command.AddParameter("created_at", request.CreatedAt, NpgsqlDbType.TimestampTz);
                command.AddParameter("expires_at", request.ExpiresAt, NpgsqlDbType.TimestampTz);
            });

        await command.AsNonQueryAsync(cancellationToken);
    }

    public async Task<PendingEmailRequest?> FindAsync(string correlationId, CancellationToken cancellationToken)
    {
        const string sql = """
                           select correlation_id,
                                  user_id,
                                  purpose,
                                  payload_json::text as payload_json,
                                  status,
                                  created_at,
                                  expires_at,
                                  completed_at,
                                  resolved_email,
                                  error
                           from notification.pending_email_requests
                           where correlation_id = @correlation_id;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command => command.AddParameter("correlation_id", correlationId));

        return await command.ReadOnce(
            r =>
            {
                string corrId = r.GetString(r.GetOrdinal("correlation_id"));
                long userId = r.GetInt64(r.GetOrdinal("user_id"));
                string purpose = r.GetString(r.GetOrdinal("purpose"));
                string payloadJson = r.GetString(r.GetOrdinal("payload_json"));
                string statusCode = r.GetString(r.GetOrdinal("status"));
                PendingEmailRequestStatus status = EnumDatabaseCodeConverter<PendingEmailRequestStatus>.FromDatabaseCode(statusCode);
                DateTime createdAt = r.GetFieldValue<DateTime>(r.GetOrdinal("created_at"));
                DateTime expiresAt = r.GetFieldValue<DateTime>(r.GetOrdinal("expires_at"));
                DateTime? completedAt = r.IsDBNull(r.GetOrdinal("completed_at"))
                    ? null
                    : r.GetFieldValue<DateTime>(r.GetOrdinal("completed_at"));
                string? resolvedEmail = r.IsDBNull(r.GetOrdinal("resolved_email"))
                    ? null
                    : r.GetString(r.GetOrdinal("resolved_email"));
                string? error = r.IsDBNull(r.GetOrdinal("error"))
                    ? null
                    : r.GetString(r.GetOrdinal("error"));

                return new PendingEmailRequest
                {
                    CorrelationId = corrId,
                    UserId = userId,
                    Purpose = purpose,
                    Payload = payloadJson,
                    Status = status,
                    CreatedAt = createdAt,
                    ExpiresAt = expiresAt,
                    CompletedAt = completedAt,
                    ResolvedEmail = resolvedEmail,
                    Error = error,
                };
            },
            cancellationToken);
    }

    public async Task MarkCompletedAsync(
        string correlationId,
        string email,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           update notification.pending_email_requests
                           set status = @status,
                               resolved_email = @email,
                               completed_at = @completed_at,
                               error = null
                           where correlation_id = @correlation_id;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command =>
            {
                command.AddParameter("correlation_id", correlationId);
                command.AddParameter("email", email);
                command.AddParameter("completed_at", occurredAt, NpgsqlDbType.TimestampTz);
                command.AddParameter("status", EnumDatabaseCodeConverter<PendingEmailRequestStatus>
                    .ToDatabaseCode(PendingEmailRequestStatus.Completed));
            });

        await command.AsNonQueryAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        string correlationId,
        string errorMessage,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           update notification.pending_email_requests
                           set status = @status,
                               error = @error,
                               completed_at = @completed_at
                           where correlation_id = @correlation_id;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command =>
            {
                command.AddParameter("correlation_id", correlationId);
                command.AddParameter("error", errorMessage);
                command.AddParameter("completed_at", occurredAt, NpgsqlDbType.TimestampTz);
                command.AddParameter("status", EnumDatabaseCodeConverter<PendingEmailRequestStatus>
                    .ToDatabaseCode(PendingEmailRequestStatus.Failed));
            });

        await command.AsNonQueryAsync(cancellationToken);
    }
}
