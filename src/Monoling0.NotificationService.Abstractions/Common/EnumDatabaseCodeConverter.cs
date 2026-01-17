using System.Globalization;

namespace Monoling0.NotificationService.Common;

public static class EnumDatabaseCodeConverter<T> where T : struct, Enum
{
    public static short ToDatabaseCode(T value)
    {
        return Convert.ToInt16(value, CultureInfo.InvariantCulture);
    }

    public static T FromDatabaseCode(short code)
    {
        object rawValue = Convert.ChangeType(code, Enum.GetUnderlyingType(typeof(T)), CultureInfo.InvariantCulture);

        if (!Enum.IsDefined(typeof(T), rawValue))
            throw new InvalidOperationException($"Enum value {code} is not defined.");

        return (T)Enum.ToObject(typeof(T), rawValue);
    }
}
