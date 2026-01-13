namespace Monoling0.NotificationService.Common;

public static class EnumDatabaseCodeConverter<T> where T : struct, Enum
{
    private static readonly Lazy<EnumMapping> EnumMappings = new(InitializeMappings());

    private sealed class EnumMapping
    {
        public required IReadOnlyDictionary<T, string> EnumToCode { get; init; }

        public required IReadOnlyDictionary<string, T> CodeToEnum { get; init; }
    }

    public static string ToDatabaseCode(T value)
    {
        return EnumMappings.Value.EnumToCode.TryGetValue(value, out string? code)
            ? code
            : throw new InvalidOperationException($"Enum value {value} is not defined.");
    }

    public static T FromDatabaseCode(string code)
    {
        return EnumMappings.Value.CodeToEnum.TryGetValue(code, out T value)
            ? value
            : throw new InvalidOperationException($"Enum value {code} is not defined.");
    }

    private static EnumMapping InitializeMappings()
    {
        var enumToCode = new Dictionary<T, string>();
        var codeToEnum = new Dictionary<string, T>();

        foreach (T enumValue in Enum.GetValues<T>())
        {
            string? description = enumValue.GetDescription();

            if (string.IsNullOrWhiteSpace(description))
                throw new InvalidOperationException($"Enum value {enumValue} does not have a description");

            enumToCode[enumValue] = description;
            codeToEnum[description] = enumValue;
        }

        return new EnumMapping { EnumToCode = enumToCode, CodeToEnum = codeToEnum };
    }
}
