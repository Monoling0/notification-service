using System.ComponentModel;
using System.Reflection;

namespace Monoling0.NotificationService.Common;

public static class EnumExtensions
{
    public static string? GetDescription(this Enum value)
    {
        FieldInfo? field = value.GetType().GetField(value.ToString());

        DescriptionAttribute? descriptionAttribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return descriptionAttribute?.Description;
    }
}
