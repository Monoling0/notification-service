using Monoling0.NotificationService.Messaging.Kafka.Models;

namespace Monoling0.NotificationService.UseCases;

public interface IEventHandler<T>
{
    Task HandleAsync(KafkaConsumedMessage<T> message, CancellationToken cancellationToken);
}
