using Common;

namespace Monoling0.NotificationService.Messaging.Kafka.Metadata;

public interface IKafkaEnvelopeMetadataAccessor<in T>
{
    EventMeta? GetMeta(T envelope);
}
