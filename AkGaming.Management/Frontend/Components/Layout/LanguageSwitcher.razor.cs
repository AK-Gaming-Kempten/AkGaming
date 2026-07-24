using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Layout;

public partial class LanguageSwitcher : ComponentBase
{
    private bool IsEnglish => CultureInfo.CurrentUICulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase);
    private bool IsGerman => CultureInfo.CurrentUICulture.Name.StartsWith("de", StringComparison.OrdinalIgnoreCase);
    private string LanguageCssClass => IsGerman ? "language-german" : "language-english";

    private void SelectEnglish()
    {
        SelectCulture("en-GB");
    }

    private void SelectGerman()
    {
        SelectCulture("de-DE");
    }

    private void SelectCulture(string culture)
    {
        var relativeUrl = Navigation.ToBaseRelativePath(Navigation.Uri);
        var returnUrl = "/" + relativeUrl;
        var cultureUrl = $"localization/set?culture={Uri.EscapeDataString(culture)}&returnUrl={Uri.EscapeDataString(returnUrl)}";
        Navigation.NavigateTo(cultureUrl, forceLoad: true);
    }
}
