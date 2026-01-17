using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Monoling0.NotificationService.Messaging.Headers;

public sealed class MessageHeaders : IReadOnlyDictionary<string, byte[]>
{
    private readonly Dictionary<string, byte[]> _headers;

    public MessageHeaders()
    {
        _headers = new Dictionary<string, byte[]>();
    }

    public MessageHeaders(IEnumerable<KeyValuePair<string, byte[]>> headers)
    {
        _headers = new Dictionary<string, byte[]>(headers);
    }

    public int Count => _headers.Count;

    public byte[] this[string key] => _headers[key];

    public IEnumerable<string> Keys => _headers.Keys;

    public IEnumerable<byte[]> Values => _headers.Values;

    public bool ContainsKey(string key)
    {
        return _headers.ContainsKey(key);
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out byte[] value)
    {
        return _headers.TryGetValue(key, out value);
    }

    public IEnumerator<KeyValuePair<string, byte[]>> GetEnumerator()
    {
        return _headers.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public MessageHeaders With(string key, byte[] value)
    {
        _headers[key] = value;
        return this;
    }

    public MessageHeaders WithUtf8(string key, string value)
    {
        _headers[key] = Encoding.UTF8.GetBytes(value);
        return this;
    }

    public byte[]? TryGetBytes(string key)
    {
        return _headers.GetValueOrDefault(key);
    }

    public string? TryGetUtf8(string key)
    {
        return _headers.TryGetValue(key, out byte[]? value) ? Encoding.UTF8.GetString(value) : null;
    }
}
