using Common;
using Monoling0.NotificationService.Messaging.Kafka.Metadata;

namespace Monoling0.NotificationService.Presentation.Kafka.Metadata;

public sealed class ProtobufEnvelopeMetadataAccessor<T> : IKafkaEnvelopeMetadataAccessor<T>
{
    private readonly Func<T, EventMeta?> _accessor;

    public ProtobufEnvelopeMetadataAccessor(Func<T, EventMeta?> accessor)
    {
        _accessor = accessor;
    }

    public EventMeta? GetMeta(T envelope)
    {
        return _accessor(envelope);
    }
}
