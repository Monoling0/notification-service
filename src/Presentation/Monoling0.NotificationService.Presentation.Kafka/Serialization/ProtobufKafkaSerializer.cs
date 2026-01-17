using Google.Protobuf;
using Monoling0.NotificationService.Messaging.Kafka;

namespace Monoling0.NotificationService.Presentation.Kafka.Serialization;

public sealed class ProtobufKafkaSerializer<T> : IKafkaSerializer<T> where T : class, IMessage<T>, new()
{
    public byte[] Serialize(T message)
    {
        return message.ToByteArray();
    }

    public T Deserialize(ReadOnlySpan<byte> bytes)
    {
        var message = new T();
        message.MergeFrom(bytes);
        return message;
    }
}
