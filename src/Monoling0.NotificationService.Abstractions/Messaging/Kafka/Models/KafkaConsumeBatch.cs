namespace Monoling0.NotificationService.Messaging.Kafka.Models;

public sealed class KafkaConsumeBatch<T>
{
    private readonly Func<CancellationToken, Task> _commit;

    public KafkaConsumeBatch(
        IReadOnlyCollection<KafkaConsumedMessage<T>> messages,
        Func<CancellationToken, Task> commit)
    {
        Messages = messages;
        _commit = commit;
    }

    public IReadOnlyCollection<KafkaConsumedMessage<T>> Messages { get; }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        return _commit(cancellationToken);
    }
}
