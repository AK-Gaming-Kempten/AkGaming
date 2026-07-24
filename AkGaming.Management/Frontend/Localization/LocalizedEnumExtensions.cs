using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;

namespace AkGaming.Management.Frontend.Localization;

public static partial class LocalizedEnumExtensions
{
    public static string ToLocalizedString(this Enum value, IStringLocalizer<SharedStrings> localizer)
    {
        var key = $"Status_{value}";
        var localized = localizer[key];
        if (!localized.ResourceNotFound)
        {
            return localized.Value;
        }

        return PascalCaseBoundary().Replace(value.ToString(), " $1");
    }

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex PascalCaseBoundary();
}
