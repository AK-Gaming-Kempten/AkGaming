using System.Globalization;
using System.Resources;

namespace AkGaming.GamelyBot.Infrastructure;

public sealed class BotLocalizationOptions
{
    public const string SectionName = "Localization";
    public const string DefaultCulture = "en-GB";
    public static readonly IReadOnlySet<string> SupportedCultures =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "en-GB", "de-DE" };

    public string Culture { get; set; } = DefaultCulture;
}

public sealed class BotText
{
    private static readonly ResourceManager Resources =
        new("AkGaming.GamelyBot.Resources.BotResources", typeof(BotText).Assembly);

    public BotText(BotLocalizationOptions options)
    {
        Culture = CultureInfo.GetCultureInfo(options.Culture);
    }

    public CultureInfo Culture { get; }

    public string this[string key] => Resources.GetString(key, Culture) ?? key;

    public string Format(string key, params object?[] arguments)
    {
        return string.Format(Culture, this[key], arguments);
    }

    internal static BotText English { get; } =
        new(new BotLocalizationOptions { Culture = BotLocalizationOptions.DefaultCulture });
}
