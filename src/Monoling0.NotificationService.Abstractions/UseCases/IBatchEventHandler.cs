using Monoling0.NotificationService.Messaging.Kafka.Models;

namespace Monoling0.NotificationService.UseCases;

public interface IBatchEventHandler<T>
{
    Task HandleBatchAsync(IReadOnlyCollection<KafkaConsumedMessage<T>> messages, CancellationToken cancellationToken);
}