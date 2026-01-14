namespace Monoling0.NotificationService.Serialization;

public interface IJsonSerializer
{
    string Serialize<T>(T value);

    T Deserialize<T>(string json);
}
