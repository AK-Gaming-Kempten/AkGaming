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

    [HttpGet("by-discord/{discordUserId}")]
    public async Task<ActionResult<DiscordUserLinkResponse>> GetUserByDiscordId(string discordUserId, CancellationToken cancellationToken)
    {
        var discordLink = await repository.GetExternalLoginAsync("discord", discordUserId, cancellationToken);
        if (discordLink is null)
            return NotFound();
        var user = await repository.GetUserByIdAsync(discordLink.UserId, cancellationToken);
        if (user is null)
            return NotFound();
        var displayName = string.IsNullOrWhiteSpace(user.Username) ? user.Email : user.Username;
        var canAccessBoardMeetings = user.UserRoles
            .SelectMany(userRole => userRole.Role.RolePermissions)
            .Any(rolePermission => rolePermission.Permission.Key == "management.board-meetings.read");
        var canManageBoardMeetings = user.UserRoles
            .SelectMany(userRole => userRole.Role.RolePermissions)
            .Any(rolePermission => rolePermission.Permission.Key == "management.board-meetings.manage");
        var response = new DiscordUserLinkResponse(user.Id, displayName, true, canAccessBoardMeetings,
            canManageBoardMeetings);
        return Ok(response);
    }
}
