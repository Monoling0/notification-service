using Monoling0.NotificationService.Messaging.Headers;
using Monoling0.NotificationService.Observability;
using System.Diagnostics.CodeAnalysis;

namespace Monoling0.NotificationService.Presentation.Kafka.Headers;

public sealed class MessageHeadersCarrier : IHeaderCarrier
{
    private readonly MessageHeaders _headers;

    public MessageHeadersCarrier(MessageHeaders headers)
    {
        _headers = headers;
    }

    public void SetHeader(string key, byte[] value)
    {
        _headers.With(key, value);
    }

    public bool TryGetHeader(string key, [MaybeNullWhen(false)] out byte[] value)
    {
        return _headers.TryGetValue(key, out value);
    }

    public IEnumerable<KeyValuePair<string, byte[]>> GetHeaders()
    {
        return _headers;
    }
}
