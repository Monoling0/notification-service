using System.Diagnostics.CodeAnalysis;

namespace Monoling0.NotificationService.Observability;

public interface IHeaderCarrier
{
    void SetHeader(string key, byte[] value);

    bool TryGetHeader(string key, [MaybeNullWhen(false)] out byte[] value);

    IEnumerable<KeyValuePair<string, byte[]>> GetHeaders();
}
