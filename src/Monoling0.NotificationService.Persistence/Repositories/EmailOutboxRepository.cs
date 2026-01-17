using Monoling0.NotificationService.Common;
using Monoling0.NotificationService.Persistence.Database;
using Monoling0.NotificationService.Persistence.Extensions;
using Monoling0.NotificationService.Persistence.Models.Outbox;
using Monoling0.NotificationService.Persistence.Transactions;
using Npgsql;
using NpgsqlTypes;

namespace Monoling0.NotificationService.Persistence.Repositories;

public class EmailOutboxRepository : IEmailOutboxRepository
{
    private readonly NotificationDatabaseDataSource _notificationDatabaseDataSource;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public EmailOutboxRepository(
        NotificationDatabaseDataSource notificationDatabaseDataSource,
        IUnitOfWorkFactory unitOfWorkFactory)
    {
        _notificationDatabaseDataSource = notificationDatabaseDataSource;
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<long> EnqueueAsync(EmailOutboxEntry message, CancellationToken cancellationToken)
    {
        const string sql = """
                           insert into notification.email_outbox
                                (kind, recipient_user_id, to_email, subject, body, status, attempt_count, created_at, next_attempt_at)
                           values
                                (@kind, @recipient_user_id, @to_email, @subject, @body, @status, 0, @created_at, @next_attempt_at)
                           returning outbox_id;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command =>
            {
                command.AddParameter("kind", message.Kind);
                command.AddParameter("recipient_user_id", message.RecipientUserId);
                command.AddParameter("to_email", message.ToEmail);
                command.AddParameter("subject", message.Subject);
                command.AddParameter("body", message.Body);
                command.AddParameter(
                    "status", EnumDatabaseCodeConverter<EmailOutboxStatus>.ToDatabaseCode(EmailOutboxStatus.Pending));
                command.AddParameter("created_at", message.CreatedAt, NpgsqlDbType.TimestampTz);
                command.AddParameter("next_attempt_at", message.NextAttemptAt, NpgsqlDbType.TimestampTz);
            });

        return await command.AsScalarAsync<long>(cancellationToken);
    }

    public async Task<IReadOnlyCollection<EmailOutboxEntry>> DequeueAsync(
        int batchSize, CancellationToken cancellationToken)
    {
        const string sql = """
                           with cte as (
                               select outbox_id
                               from notification.email_outbox
                               where status = @pending and next_attempt_at <= now()
                               order by next_attempt_at asc, outbox_id asc
                               limit @batch_size
                               for update skip locked
                           )
                           update notification.email_outbox o
                           set status = @sending,
                               attempt_count = o.attempt_count + 1,
                               last_attempt_at = now()
                           from cte
                           where o.outbox_id = cte.outbox_id
                           returning
                               o.outbox_id,
                               o.kind,
                               o.recipient_user_id,
                               o.to_email,
                               o.subject,
                               o.body,
                               o.status,
                               o.attempt_count,
                               o.created_at,
                               o.next_attempt_at,
                               o.last_attempt_at,
                               o.sent_at,
                               o.last_error;
                           """;

        await using IUnitOfWork unitOfWork = await _unitOfWorkFactory.BeginAsync(cancellationToken);

        try
        {
            var connection = (NpgsqlConnection)unitOfWork.Connection;
            var transaction = (NpgsqlTransaction)unitOfWork.Transaction;

            await using NpgsqlCommand command = new NpgsqlCommand(sql, connection, transaction)
                .With(command =>
                {
                    command.AddParameter(
                        "pending",
                        EnumDatabaseCodeConverter<EmailOutboxStatus>.ToDatabaseCode(EmailOutboxStatus.Pending));
                    command.AddParameter(
                        "sending",
                        EnumDatabaseCodeConverter<EmailOutboxStatus>.ToDatabaseCode(EmailOutboxStatus.Sending));
                    command.AddParameter("batch_size", batchSize);
                });

            List<EmailOutboxEntry> items = await command.ReadMany(Map, cancellationToken);

            await unitOfWork.CommitAsync(cancellationToken);

            return items;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task MarkAsSentAsync(long outboxMessageId, CancellationToken cancellationToken)
    {
        const string sql = """
                           update notification.email_outbox
                           set status = @sent,
                               sent_at = now(),
                               last_error = null
                           where outbox_id = @outbox_id;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command =>
            {
                command.AddParameter("outbox_id", outboxMessageId);
                command.AddParameter(
                    "sent", EnumDatabaseCodeConverter<EmailOutboxStatus>.ToDatabaseCode(EmailOutboxStatus.Sent));
            });

        await command.AsNonQueryAsync(cancellationToken);
    }

    public async Task MarkAsFailedAsync(long outboxMessageId, string errorMessage, CancellationToken cancellationToken)
    {
        const string sql = """
                           update notification.email_outbox
                           set status = @failed,
                               last_error = @error
                           where outbox_id = @outbox_id;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command =>
            {
                command.AddParameter("outbox_id", outboxMessageId);
                command.AddParameter(
                    "failed", EnumDatabaseCodeConverter<EmailOutboxStatus>.ToDatabaseCode(EmailOutboxStatus.Failed));
                command.AddParameter("error", errorMessage);
            });

        await command.AsNonQueryAsync(cancellationToken);
    }

    public async Task RescheduleAsync(
        long outboxMessageId,
        DateTime nextAttemptAt,
        string? lastError,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           update notification.email_outbox
                           set status = @pending,
                               next_attempt_at = @next_attempt_at,
                               last_error = @last_error
                           where outbox_id = @outbox_id;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command =>
            {
                command.AddParameter("outbox_id", outboxMessageId);
                command.AddParameter(
                    "pending", EnumDatabaseCodeConverter<EmailOutboxStatus>.ToDatabaseCode(EmailOutboxStatus.Pending));
                command.AddParameter("last_error", lastError);
                command.AddParameter("next_attempt_at", nextAttemptAt);
            });

        await command.AsNonQueryAsync(cancellationToken);
    }

    private static EmailOutboxEntry Map(NpgsqlDataReader reader)
    {
        short statusCode = reader.GetInt16(reader.GetOrdinal("status"));
        EmailOutboxStatus status = EnumDatabaseCodeConverter<EmailOutboxStatus>.FromDatabaseCode(statusCode);

        return new EmailOutboxEntry
        {
            OutboxId = reader.GetInt64(reader.GetOrdinal("outbox_id")),
            Kind = reader.GetString(reader.GetOrdinal("kind")),
            RecipientUserId = reader.IsDBNull(reader.GetOrdinal("recipient_user_id"))
                ? null
                : reader.GetInt64(reader.GetOrdinal("recipient_user_id")),
            ToEmail = reader.GetString(reader.GetOrdinal("to_email")),
            Subject = reader.GetString(reader.GetOrdinal("subject")),
            Body = reader.GetString(reader.GetOrdinal("body")),
            Status = status,
            AttemptsCount = reader.GetInt32(reader.GetOrdinal("attempt_count")),
            CreatedAt = reader.GetFieldValue<DateTime>(reader.GetOrdinal("created_at")),
            NextAttemptAt = reader.GetFieldValue<DateTime>(reader.GetOrdinal("next_attempt_at")),
            LastAttemptAt = reader.IsDBNull(reader.GetOrdinal("last_attempt_at"))
                ? null
                : reader.GetFieldValue<DateTime>(reader.GetOrdinal("last_attempt_at")),
            SentAt = reader.IsDBNull(reader.GetOrdinal("sent_at"))
                ? null
                : reader.GetFieldValue<DateTime>(reader.GetOrdinal("sent_at")),
            LastError = reader.IsDBNull(reader.GetOrdinal("last_error"))
                ? null
                : reader.GetString(reader.GetOrdinal("last_error")),
        };
    }
}
