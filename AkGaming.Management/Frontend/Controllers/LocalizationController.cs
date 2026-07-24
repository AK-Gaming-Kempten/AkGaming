using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Management.Frontend.Controllers;

[Route("localization")]
public sealed class LocalizationController : Controller
{
    private static readonly HashSet<string> SupportedCultures = new(StringComparer.OrdinalIgnoreCase)
    {
        "en-GB",
        "de-DE"
    };

    [HttpGet("set")]
    public IActionResult SetCulture(string culture, string? returnUrl)
    {
        var selectedCulture = SupportedCultures.Contains(culture) ? culture : "en-GB";
        var requestCulture = new RequestCulture(selectedCulture);
        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(requestCulture);
        var cookieOptions = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps
        };

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            cookieValue,
            cookieOptions);

        var destination = Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
        return LocalRedirect(destination);
    }
}
