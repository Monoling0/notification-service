using Monoling0.NotificationService.Messaging.Headers;
using Monoling0.NotificationService.Observability;

namespace Monoling0.NotificationService.Messaging.Kafka.Models;

public sealed class KafkaConsumedMessage<T>
{
    public required string? EventId { get; init; }

    public required T Event { get; init; }

    public required KafkaMessagePosition Position { get; init; }

    public DateTime Timestamp { get; init; }

    public KafkaMessageKey Key { get; init; } = new(null);

    public MessageHeaders Headers { get; init; } = new();

    public TracingContext TracingContext { get; init; } = new(null, null, null);

    public byte[]? Payload { get; init; }
}
