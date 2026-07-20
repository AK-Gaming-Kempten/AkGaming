using AkGaming.Core.Notifications;
using AkGaming.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Identity.Api.Controllers;

[ApiController]
[Route("internal/discord-links")]
[Authorize(Policy = "IdentityDiscordLinks")]
public sealed class InternalDiscordLinksController(IIdentityRepository repository) : ControllerBase
{
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<DiscordLinkResponse>> GetDiscordLink(Guid userId, CancellationToken cancellationToken)
    {
        var user = await repository.GetUserByIdWithExternalLoginsAsync(userId, cancellationToken);
        if (user is null)
            return NotFound();

        var discordLink = user.ExternalLogins
            .Where(link => string.Equals(link.Provider, "discord", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(link => link.LinkedAtUtc)
            .FirstOrDefault();
        var response = new DiscordLinkResponse(userId, discordLink?.ProviderUserId, discordLink is not null);
        return Ok(response);
    }
}
