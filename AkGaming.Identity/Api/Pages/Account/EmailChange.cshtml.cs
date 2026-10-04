using AkGaming.Identity.Api.Authentication;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace AkGaming.Identity.Api.Pages.Account;

[EnableRateLimiting("auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EmailChangeModel(IEmailChangeService service) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;
    public string? ErrorMessage { get; private set; }
    public bool Completed { get; private set; }

    public void OnGet()
    {
        SetPrivacyHeaders();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        SetPrivacyHeaders();
        try
        {
            await service.ConfirmAsync(Token, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
            await LocalSessionManager.SignOutAsync(HttpContext);
            Completed = true;
            Token = string.Empty;
            ModelState.Clear();
        }
        catch (AuthException exception)
        {
            ErrorMessage = exception.Message;
        }
        return Page();
    }

    private void SetPrivacyHeaders()
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    }
}
