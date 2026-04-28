using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Tournaments.WebApi.Controllers;

[ApiController]
[Route("api/teams")]
[Tags("Teams")]
public sealed class TeamsController(ITeamManagementService service) : ControllerBase
{
    [HttpPost(Name = "CreateTeam")]
    [EndpointSummary("Create a team and assign the creator as owner.")]
    [ProducesResponseType<TeamDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDto>> CreateTeam(
        CreateTeamRequest request,
        CancellationToken cancellationToken)
    {
        var team = await service.CreateTeamAsync(request.ActingUserId, request.GameId, request.Name, cancellationToken);
        return Ok(team);
    }

    [HttpGet("{teamId:guid}", Name = "GetTeam")]
    [EndpointSummary("Get a team with memberships and guest profiles.")]
    [ProducesResponseType<TeamDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamDto>> GetTeam(Guid teamId, CancellationToken cancellationToken)
    {
        var team = await service.GetTeamAsync(teamId, cancellationToken);
        return team is null ? NotFound() : Ok(team);
    }

    [HttpGet("/api/users/{userId}/teams", Name = "GetUserTeams")]
    [EndpointSummary("List teams that the user is a member of.")]
    [ProducesResponseType<IReadOnlyList<TeamDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeamDto>>> GetUserTeams(
        string userId,
        CancellationToken cancellationToken)
    {
        var teams = await service.GetTeamsForUserAsync(userId, cancellationToken);
        return Ok(teams);
    }

    [HttpPost("{teamId:guid}/members", Name = "AddTeamMember")]
    [EndpointSummary("Add a user to a team.")]
    [ProducesResponseType<TeamDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDto>> AddTeamMember(
        Guid teamId,
        AddTeamMemberRequest request,
        CancellationToken cancellationToken)
    {
        var team = await service.AddMemberAsync(teamId, request.ActingUserId, request.UserId, request.Role, cancellationToken);
        return Ok(team);
    }

    [HttpPut("{teamId:guid}/members/{userId}", Name = "UpdateTeamMemberRole")]
    [EndpointSummary("Update a team member role.")]
    [ProducesResponseType<TeamDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDto>> UpdateTeamMemberRole(
        Guid teamId,
        string userId,
        UpdateTeamMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        var team = await service.UpdateMemberRoleAsync(teamId, request.ActingUserId, userId, request.Role, cancellationToken);
        return Ok(team);
    }

    [HttpPut("{teamId:guid}/logo", Name = "UpdateTeamLogo")]
    [EndpointSummary("Set or clear a team's logo asset.")]
    [ProducesResponseType<TeamDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDto>> UpdateTeamLogo(
        Guid teamId,
        UpdateTeamLogoRequest request,
        CancellationToken cancellationToken)
    {
        var team = await service.UpdateTeamLogoAsync(teamId, request.ActingUserId, request.LogoAssetId, cancellationToken);
        return Ok(team);
    }

    [HttpPut("{teamId:guid}", Name = "UpdateTeam")]
    [EndpointSummary("Update editable team details.")]
    [ProducesResponseType<TeamDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDto>> UpdateTeam(
        Guid teamId,
        UpdateTeamRequest request,
        CancellationToken cancellationToken)
    {
        var team = await service.UpdateTeamAsync(teamId, request.ActingUserId, request.Name, cancellationToken);
        return Ok(team);
    }

    [HttpGet("{teamId:guid}/available-player-profiles/{gameId}", Name = "GetAvailableTeamProfiles")]
    [EndpointSummary("List all player profiles the team can use for a game.")]
    [ProducesResponseType<IReadOnlyList<PlayerProfileDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlayerProfileDto>>> GetAvailableTeamProfiles(
        Guid teamId,
        string gameId,
        CancellationToken cancellationToken)
    {
        var profiles = await service.GetAvailableProfilesAsync(teamId, gameId, cancellationToken);
        return Ok(profiles);
    }

    [HttpPost("{teamId:guid}/guest-player-profiles", Name = "CreateGuestPlayerProfile")]
    [EndpointSummary("Create a guest player profile owned by the team.")]
    [ProducesResponseType<PlayerProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PlayerProfileDto>> CreateGuestPlayerProfile(
        Guid teamId,
        CreateGuestPlayerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await service.CreateGuestPlayerProfileAsync(teamId, request.ActingUserId, request.Name, request.RankRating, cancellationToken);
        return Ok(profile);
    }

    [HttpPut("{teamId:guid}/guest-player-profiles/{playerProfileId:guid}", Name = "UpdateGuestPlayerProfile")]
    [EndpointSummary("Update a guest player profile.")]
    [ProducesResponseType<PlayerProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PlayerProfileDto>> UpdateGuestPlayerProfile(
        Guid teamId,
        Guid playerProfileId,
        UpdateGuestPlayerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await service.UpdateGuestPlayerProfileAsync(teamId, playerProfileId, request.ActingUserId, request.Name, request.RankRating, cancellationToken);
        return Ok(profile);
    }

    [HttpDelete("{teamId:guid}/guest-player-profiles/{playerProfileId:guid}", Name = "DeleteGuestPlayerProfile")]
    [EndpointSummary("Delete a guest player profile.")]
    [ProducesResponseType<TeamDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDto>> DeleteGuestPlayerProfile(
        Guid teamId,
        Guid playerProfileId,
        [FromQuery] string actingUserId,
        CancellationToken cancellationToken)
    {
        var team = await service.DeleteGuestPlayerProfileAsync(teamId, playerProfileId, actingUserId, cancellationToken);
        return Ok(team);
    }
}

public sealed record CreateTeamRequest(string ActingUserId, string GameId, string Name);
public sealed record AddTeamMemberRequest(string ActingUserId, string UserId, TeamRoleDto Role);
public sealed record UpdateTeamMemberRoleRequest(string ActingUserId, TeamRoleDto Role);
public sealed record UpdateTeamRequest(string ActingUserId, string Name);
public sealed record UpdateTeamLogoRequest(string ActingUserId, Guid? LogoAssetId);
public sealed record CreateGuestPlayerProfileRequest(string ActingUserId, string Name, int? RankRating = null);
public sealed record UpdateGuestPlayerProfileRequest(string ActingUserId, string Name, int? RankRating = null);
