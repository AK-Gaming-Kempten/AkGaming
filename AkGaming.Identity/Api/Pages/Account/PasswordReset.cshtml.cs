using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Contracts.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AkGaming.Identity.Api.Pages.Account;

public sealed class PasswordResetModel : PageModel
{
    private readonly IAuthService _authService;

    public PasswordResetModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Token { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet(string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            Token = token;
        }
    }

    public async Task<IActionResult> OnPostRequestResetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RequestPasswordResetAsync(
                new PasswordResetRequest(Email),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            StatusMessage = response.ResetToken is null
                ? "If the account exists, a password reset email was sent."
                : $"Password reset email sent. Token: {response.ResetToken}";
        }
        catch (AuthException exception)
        {
            ErrorMessage = exception.Message;
        }

        return RedirectToPage(new { returnUrl = ReturnUrl });
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _authService.ResetPasswordAsync(
                new ResetPasswordRequest(Token, Password),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            var target = string.IsNullOrWhiteSpace(ReturnUrl)
                ? "/account/login?error=Password%20reset.%20You%20can%20now%20sign%20in."
                : $"/account/login?returnUrl={Uri.EscapeDataString(ReturnUrl)}&error=Password%20reset.%20You%20can%20now%20sign%20in.";

            return Redirect(target);
        }
        catch (AuthException exception)
        {
            ErrorMessage = exception.Message;
            return RedirectToPage(new { token = Token, returnUrl = ReturnUrl });
        }
    }
}
