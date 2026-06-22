using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Tournaments.WebApi.Controllers;

[ApiController]
[Route("api/users/{userId}/player-profiles")]
[Tags("Player Profiles")]
public sealed class PlayerProfilesController(IPlayerProfileManagementService service) : ControllerBase
{
    [HttpGet(Name = "GetUserPlayerProfiles")]
    [Authorize(Policy = "PlayerProfilesManageOrSelfRouteUserId")]
    [EndpointSummary("List player profiles for a user.")]
    [ProducesResponseType<IReadOnlyList<PlayerProfileDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlayerProfileDto>>> GetUserPlayerProfiles(
        string userId,
        CancellationToken cancellationToken)
    {
        var profiles = await service.GetUserProfilesAsync(userId, cancellationToken);
        return Ok(profiles);
    }

    [HttpPut("{gameId}", Name = "UpsertUserPlayerProfile")]
    [Authorize(Policy = "PlayerProfilesManageOrSelfRouteUserId")]
    [EndpointSummary("Create or update a user player profile for a game.")]
    [ProducesResponseType<PlayerProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PlayerProfileDto>> UpsertUserPlayerProfile(
        string userId,
        string gameId,
        UpsertUserPlayerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await service.UpsertUserProfileAsync(userId, gameId, request.Name, request.RankRating, request.ProfileLink, cancellationToken);
        return Ok(profile);
    }

    [HttpPut("{gameId}/logo", Name = "UpdateUserPlayerProfileLogo")]
    [Authorize(Policy = "PlayerProfilesManageOrSelfRouteUserId")]
    [EndpointSummary("Set or clear a user player profile logo.")]
    [ProducesResponseType<PlayerProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PlayerProfileDto>> UpdateUserPlayerProfileLogo(
        string userId,
        string gameId,
        UpdateUserPlayerProfileLogoRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await service.UpdateUserProfileLogoAsync(userId, gameId, request.LogoAssetId, cancellationToken);
        return Ok(profile);
    }
}

public sealed record UpsertUserPlayerProfileRequest(string Name, int? RankRating = null, string? ProfileLink = null);
public sealed record UpdateUserPlayerProfileLogoRequest(Guid? LogoAssetId);
