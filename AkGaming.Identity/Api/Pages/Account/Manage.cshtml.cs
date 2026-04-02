using System.Security.Claims;
using AkGaming.Identity.Api.Authentication;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AkGaming.Identity.Api.Pages.Account;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public sealed class ManageModel : PageModel
{
    private readonly IAuthService _authService;

    public ManageModel(IAuthService authService)
    {
        _authService = authService;
    }

    public CurrentUserResponse? Profile { get; private set; }

    [BindProperty]
    public string VerificationToken { get; set; } = string.Empty;

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(Status))
        {
            StatusMessage = Status;
        }

        await LoadProfileAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostSendVerificationAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var response = await _authService.RequestEmailVerificationForUserAsync(userId, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        StatusMessage = response.Message;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostVerifyEmailAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _authService.VerifyEmailAsync(new VerifyEmailRequest(VerificationToken), HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
            StatusMessage = "Email verified.";
        }
        catch (AuthException exception)
        {
            StatusMessage = exception.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostStartDiscordLinkAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var response = await _authService.GetDiscordLinkUrlAsync(userId, cancellationToken);
        return Redirect(response.AuthorizationUrl);
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await LocalSessionManager.SignOutAsync(HttpContext);
        return Redirect("/account/login");
    }

    private async Task LoadProfileAsync(CancellationToken cancellationToken)
    {
        Profile = await _authService.GetCurrentUserAsync(GetUserId(), cancellationToken);
    }

    private Guid GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.Parse(raw!);
    }
}
