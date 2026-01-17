namespace Monoling0.NotificationService.Messaging.Kafka.Models;

public readonly record struct KafkaTopicName
{
    public KafkaTopicName(string topic)
    {
        ArgumentException.ThrowIfNullOrEmpty(topic);

        Value = topic;
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator KafkaTopicName(string topic) => new(topic);

    public static implicit operator string(KafkaTopicName topic) => topic.Value;
}
