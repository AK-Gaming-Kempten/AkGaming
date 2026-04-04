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
    private readonly IAuthHardeningSettings _hardeningSettings;

    public ManageModel(IAuthService authService, IAuthHardeningSettings hardeningSettings)
    {
        _authService = authService;
        _hardeningSettings = hardeningSettings;
    }

    public CurrentUserResponse? Profile { get; private set; }

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
