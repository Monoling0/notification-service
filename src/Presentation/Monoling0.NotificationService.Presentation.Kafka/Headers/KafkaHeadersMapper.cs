using Monoling0.NotificationService.Messaging.Headers;

namespace Monoling0.NotificationService.Presentation.Kafka.Headers;

public static class KafkaHeadersMapper
{
    public static MessageHeaders ToMessageHeaders(Confluent.Kafka.Headers? headers)
    {
        if (headers is null)
            return new MessageHeaders();

        IEnumerable<KeyValuePair<string, byte[]>> items = headers.Select(header =>
            new KeyValuePair<string, byte[]>(header.Key, header.GetValueBytes() ?? []));

        return new MessageHeaders(items);
    }

    public static Confluent.Kafka.Headers ToKafkaHeaders(MessageHeaders? headers)
    {
        var result = new Confluent.Kafka.Headers();

        if (headers is null)
            return result;

        foreach (KeyValuePair<string, byte[]> header in headers)
            result.Add(header.Key, header.Value);

        return result;
    }
}
