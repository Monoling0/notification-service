using Common;
using Google.Protobuf.WellKnownTypes;

namespace Monoling0.NotificationService.Application.Messaging;

public static class EventMetaFactory
{
    private const string SourceServiceName = "notification-service";

    public static EventMeta Create(string eventId, string? correlationId = null)
    {
        return new EventMeta
        {
            EventId = eventId,
            OccurredAt = Timestamp.FromDateTime(DateTime.UtcNow),
            SourceService = SourceServiceName,
            SchemaVersion = 1,
            CorrelationId = correlationId,
        };
    }
}
