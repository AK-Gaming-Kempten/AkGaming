using AkGaming.Identity.Api.Authentication;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Contracts.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AkGaming.Identity.Api.Pages.Account;

public sealed class LoginModel : PageModel
{
    private readonly IAuthService _authService;

    public LoginModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Error { get; set; }

    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(LocalSessionManager.NormalizeReturnUrl(HttpContext, ReturnUrl));
        }

        ErrorMessage = Error;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            var user = await _authService.LoginInteractiveAsync(
                new LoginRequest(Email, Password),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            await LocalSessionManager.SignInAsync(HttpContext, user);
            return Redirect(LocalSessionManager.NormalizeReturnUrl(HttpContext, ReturnUrl));
        }
        catch (AuthException exception)
        {
            ErrorMessage = exception.Message;
            return Page();
        }
    }
}
