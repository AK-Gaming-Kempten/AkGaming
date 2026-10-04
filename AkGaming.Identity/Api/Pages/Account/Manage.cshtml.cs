using System.Security.Claims;
using AkGaming.Identity.Api.Authentication;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace AkGaming.Identity.Api.Pages.Account;

[EnableRateLimiting("auth")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public sealed class ManageModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly IAuthHardeningSettings _hardeningSettings;
    private readonly IEmailChangeService _emailChangeService;

    public ManageModel(IAuthService authService, IAuthHardeningSettings hardeningSettings, IEmailChangeService emailChangeService)
    {
        _authService = authService;
        _emailChangeService = emailChangeService;
        _hardeningSettings = hardeningSettings;
    }

    public EmailChangeResponse? PendingEmailChange { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty]
    public string NewEmail { get; set; } = string.Empty;

    [BindProperty]
    public string CurrentPassword { get; set; } = string.Empty;

    public CurrentUserResponse? Profile { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(Status))
        {
            StatusMessage = Status;
        }

        await LoadProfileAsync(cancellationToken);
        if (_hardeningSettings.RequireVerifiedEmailForLogin && Profile is not null && !Profile.IsEmailVerified)
        {
            return Redirect(LocalSessionManager.BuildVerificationRedirect(HttpContext, "/account/manage", StatusMessage));
        }

        return Page();
    }

    public async Task<IActionResult> OnPostStartDiscordLinkAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var current = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        if (_hardeningSettings.RequireVerifiedEmailForLogin && !current.IsEmailVerified)
        {
            return Redirect(LocalSessionManager.BuildVerificationRedirect(HttpContext, "/account/manage"));
        }

        var response = await _authService.GetDiscordLinkUrlAsync(userId, cancellationToken);
        return Redirect(response.AuthorizationUrl);
    }

    public async Task<IActionResult> OnPostUpdateUsernameAsync(CancellationToken cancellationToken)
    {
        try
        {
            var user = await _authService.UpdateUsernameAsync(
                GetUserId(),
                Username,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            await LocalSessionManager.SignInAsync(HttpContext, user);
            StatusMessage = "Username updated.";
            return RedirectToPage(new { status = StatusMessage });
        }
        catch (AuthException exception)
        {
            StatusMessage = exception.Message;
            await LoadProfileAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRequestEmailChangeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _emailChangeService.RequestAsync(GetUserId(), NewEmail, CurrentPassword,
                HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
            StatusMessage = "Confirmation email sent. Your current email stays active until you confirm the new address.";
            return RedirectToPage();
        }
        catch (AuthException exception)
        {
            ErrorMessage = exception.Message;
            CurrentPassword = string.Empty;
            ModelState.Remove(nameof(CurrentPassword));
            await LoadProfileAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostStartDiscordEmailChangeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var url = await _emailChangeService.StartDiscordAsync(GetUserId(), NewEmail, cancellationToken);
            var state = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(url).Query)["state"].ToString();
            Response.Cookies.Append("akgaming.email-change-state", state, new CookieOptions
            {
                HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax,
                Path = "/auth/discord/callback", MaxAge = TimeSpan.FromMinutes(10), IsEssential = true
            });
            return Redirect(url);
        }
        catch (AuthException exception)
        {
            ErrorMessage = exception.Message;
            await LoadProfileAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostCancelEmailChangeAsync(CancellationToken cancellationToken)
    {
        await _emailChangeService.CancelAsync(GetUserId(), HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        StatusMessage = "Pending email change cancelled.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await LocalSessionManager.SignOutAsync(HttpContext);
        return Redirect("/account/login");
    }

    private async Task LoadProfileAsync(CancellationToken cancellationToken)
    {
        Profile = await _authService.GetCurrentUserAsync(GetUserId(), cancellationToken);
        Username = Profile.Username;
        PendingEmailChange = await _emailChangeService.GetPendingAsync(GetUserId(), cancellationToken);
    }

    private Guid GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.Parse(raw!);
    }
}
