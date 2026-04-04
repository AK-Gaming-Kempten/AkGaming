using AkGaming.Identity.Api.Authentication;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Contracts.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AkGaming.Identity.Api.Pages.Account;

public sealed class RegisterModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly IAuthHardeningSettings _hardeningSettings;

    public RegisterModel(IAuthService authService, IAuthHardeningSettings hardeningSettings)
    {
        _authService = authService;
        _hardeningSettings = hardeningSettings;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public bool PrivacyPolicyAccepted { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(LocalSessionManager.GetAuthenticatedRedirect(HttpContext, User, ReturnUrl, _hardeningSettings.RequireVerifiedEmailForLogin));
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            var user = await _authService.RegisterInteractiveAsync(
                new RegisterRequest(Email, Password, PrivacyPolicyAccepted),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            await LocalSessionManager.SignInAsync(HttpContext, user);
            return Redirect(LocalSessionManager.GetPostSignInRedirect(HttpContext, user, ReturnUrl, _hardeningSettings.RequireVerifiedEmailForLogin));
        }
        catch (AuthException exception)
        {
            ErrorMessage = exception.Message;
            return Page();
        }
    }
}
