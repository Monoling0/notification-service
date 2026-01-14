using Google.Protobuf;

namespace Monoling0.NotificationService.Messaging.Kafka;

public interface IKafkaSerializer<T> where T : class, IMessage<T>, new()
{
    byte[] Serialize(T message);

    T Deserialize(ReadOnlySpan<byte> bytes);
}
