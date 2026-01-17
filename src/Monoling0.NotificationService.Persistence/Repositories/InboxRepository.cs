using Monoling0.NotificationService.Common;
using Monoling0.NotificationService.Persistence.Database;
using Monoling0.NotificationService.Persistence.Extensions;
using Monoling0.NotificationService.Persistence.Models.Inbox;
using Npgsql;

namespace Monoling0.NotificationService.Persistence.Repositories;

public class InboxRepository : IInboxRepository
{
    private readonly NotificationDatabaseDataSource _notificationDatabaseDataSource;

    public InboxRepository(NotificationDatabaseDataSource notificationDatabaseDataSource)
    {
        _notificationDatabaseDataSource = notificationDatabaseDataSource;
    }

    public async Task<InboxDecision> TryAcquireAsync(
        string? eventId,
        InboxEventPosition position,
        CancellationToken cancellationToken)
    {
        const string sql = """
                     insert into notification.inbox_events(
                         event_id,
                         topic,
                         partition,
                         message_offset,
                         received_at,
                         status,
                         attempt_count)
                     values (@event_id, @topic, @partition, @message_offset, now(), @status, 0)
                     on conflict (event_id) do nothing;
                     """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command =>
            {
                command.AddParameter("event_id", eventId);
                command.AddParameter("topic", position.Topic);
                command.AddParameter("partition", position.Partition);
                command.AddParameter("message_offset", position.Offset);
                command.AddParameter(
                    "status", EnumDatabaseCodeConverter<InboxEventStatus>.ToDatabaseCode(InboxEventStatus.Received));
            });

        int rowsAffected = await command.AsNonQueryAsync(cancellationToken);
        return rowsAffected == 1 ? InboxDecision.Accepted : InboxDecision.Duplicate;
    }

    public async ValueTask MarkAsProcessedAsync(string? eventId, CancellationToken cancellationToken)
    {
        const string sql = """
                           update notification.inbox_events
                           set processed_at = now(), status = @status
                           where event_id = @event_id;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command =>
            {
                command.AddParameter("event_id", eventId);
                command.AddParameter(
                    "status", EnumDatabaseCodeConverter<InboxEventStatus>.ToDatabaseCode(InboxEventStatus.Processed));
            });

        await command.AsNonQueryAsync(cancellationToken);
    }

    public async Task MarkAsFailedAsync(string eventId, string errorMessage, CancellationToken cancellationToken)
    {
        const string sql = """
                           update notification.inbox_events
                           set status = @status,
                               last_error = @errorMessage,
                               attempt_count = attempt_count + 1,
                               last_attempt_at = now()
                           where event_id = @event_id;
                           """;

        await using NpgsqlCommand command = _notificationDatabaseDataSource.DataSource
            .NewCommand(sql)
            .With(command =>
            {
                command.AddParameter("event_id", eventId);
                command.AddParameter("errorMessage", errorMessage);
                command.AddParameter(
                    "status", EnumDatabaseCodeConverter<InboxEventStatus>.ToDatabaseCode(InboxEventStatus.Failed));
            });

        await command.AsNonQueryAsync(cancellationToken);
    }
}
