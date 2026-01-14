using Monoling0.NotificationService.Messaging.Kafka.Models;
using System.Threading.Channels;

namespace Monoling0.NotificationService.Messaging.Kafka;

public interface IKafkaBatchSource<T>
{
    ChannelReader<KafkaConsumeBatch<T>> Reader { get; }

    ValueTask StartAsync(CancellationToken cancellationToken);

    ValueTask StopAsync(CancellationToken cancellationToken);
}
