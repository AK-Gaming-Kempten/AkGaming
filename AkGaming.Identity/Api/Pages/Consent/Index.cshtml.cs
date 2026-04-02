using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace AkGaming.Identity.Api.Pages.Consent;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public sealed class IndexModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string ClientId { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string ClientName { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Scopes { get; set; } = string.Empty;

    public IReadOnlyList<string> ScopeItems =>
        Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public IActionResult OnGet()
    {
        if (!IsValidReturnUrl(ReturnUrl))
        {
            return Redirect("/account/manage");
        }

        return Page();
    }

    public IActionResult OnPostApprove()
    {
        if (!IsValidReturnUrl(ReturnUrl))
        {
            return Redirect("/account/manage");
        }

        return Redirect(QueryHelpers.AddQueryString(ReturnUrl, "consent", "accept"));
    }

    public IActionResult OnPostDeny()
    {
        if (!IsValidReturnUrl(ReturnUrl))
        {
            return Redirect("/account/manage");
        }

        return Redirect(QueryHelpers.AddQueryString(ReturnUrl, "consent", "deny"));
    }

    private static bool IsValidReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl)
               && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
               && returnUrl.StartsWith("/connect/authorize", StringComparison.Ordinal);
    }
}
