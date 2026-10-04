using System.Security.Claims;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenIddict.Validation.AspNetCore;

namespace AkGaming.Identity.Api.Controllers;

[ApiController]
[Route("auth/email/change")]
[EnableRateLimiting("auth")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public sealed class EmailChangeController(IEmailChangeService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var result = await service.GetPendingAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> RequestChange(RequestEmailChangeRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        try
        {
            await service.RequestAsync(userId, request.NewEmail, request.CurrentPassword, GetIpAddress(), cancellationToken);
            return Accepted(new { Message = "Confirmation email sent. Your current email remains active until confirmation." });
        }
        catch (AuthException exception)
        {
            return StatusCode(exception.StatusCode, new { Message = exception.Message });
        }
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        await service.CancelAsync(userId, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await service.ConfirmAsync(request.Token, GetIpAddress(), cancellationToken);
            return NoContent();
        }
        catch (AuthException exception)
        {
            return StatusCode(exception.StatusCode, new { Message = exception.Message });
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        var subject = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(subject, out userId);
    }

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
