using System.Security.Claims;
using AkGaming.Identity.Api.Authentication;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Contracts.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AkGaming.Identity.Api.Pages.Account;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public sealed class VerifyModel : PageModel
{
    private readonly IAuthService _authService;

    public VerifyModel(IAuthService authService)
    {
        _authService = authService;
    }

    public CurrentUserResponse? Profile { get; private set; }

    [BindProperty]
    public string VerificationToken { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

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
        if (Profile is null)
        {
            await LocalSessionManager.SignOutAsync(HttpContext);
            return Redirect("/account/login");
        }

        if (Profile.IsEmailVerified)
        {
            await LocalSessionManager.SignInAsync(HttpContext, Profile);
            return Redirect(LocalSessionManager.NormalizeReturnUrl(HttpContext, ReturnUrl));
        }

        Email = Profile.Email;
        return Page();
    }

    public async Task<IActionResult> OnPostSendVerificationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RequestEmailVerificationForUserAsync(GetUserId(), HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
            StatusMessage = response.VerificationToken is null
                ? "Verification email sent."
                : $"Verification email sent. Token: {response.VerificationToken}";
        }
        catch (AuthException exception)
        {
            StatusMessage = exception.Message;
        }

        return RedirectToPage(new { returnUrl = ReturnUrl });
    }

    public async Task<IActionResult> OnPostVerifyEmailAsync(CancellationToken cancellationToken)
    {
        try
        {
            var user = await _authService.VerifyEmailAsync(new VerifyEmailRequest(VerificationToken), HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
            await LocalSessionManager.SignInAsync(HttpContext, user);
            StatusMessage = "Email verified.";
            return Redirect(LocalSessionManager.NormalizeReturnUrl(HttpContext, ReturnUrl));
        }
        catch (AuthException exception)
        {
            StatusMessage = exception.Message;
            return RedirectToPage(new { returnUrl = ReturnUrl });
        }
    }

    public async Task<IActionResult> OnPostChangeEmailAsync(CancellationToken cancellationToken)
    {
        try
        {
            var user = await _authService.UpdatePendingVerificationEmailAsync(GetUserId(), Email, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
            await LocalSessionManager.SignInAsync(HttpContext, user);

            var verification = await _authService.RequestEmailVerificationForUserAsync(user.UserId, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
            StatusMessage = verification.VerificationToken is null
                ? "Email updated. A new verification email was sent."
                : $"Email updated. New verification token: {verification.VerificationToken}";
        }
        catch (AuthException exception)
        {
            StatusMessage = exception.Message;
        }

        return RedirectToPage(new { returnUrl = ReturnUrl });
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
