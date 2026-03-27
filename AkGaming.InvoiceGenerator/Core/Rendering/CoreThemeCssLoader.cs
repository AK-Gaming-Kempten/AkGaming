namespace AkGaming.InvoiceGenerator.Core.Rendering;

internal static class CoreThemeCssLoader
{
    public static string Load() => CoreThemeAssetLoader.LoadTextBySuffix("akgaming-base-theme.css");
}
