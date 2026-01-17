using System.Text.Json;

namespace Monoling0.NotificationService.Serialization;

public class JsonTextSerializer : IJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options) ?? throw new JsonException("Failed to deserialize JSON");
    }
}
