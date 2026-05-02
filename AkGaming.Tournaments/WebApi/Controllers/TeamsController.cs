using AkGaming.Tournaments.WebApi.Authentication;
using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Tournaments.WebApi.Controllers;

[ApiController]
[Route("api/teams")]
[Tags("Teams")]
public sealed class TeamsController(ITeamManagementService service) : ControllerBase
{
    [HttpPost(Name = "CreateTeam")]
    [Authorize]
    [EndpointSummary("Create a team and assign the creator as owner.")]
    [ProducesResponseType<TeamDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDto>> CreateTeam(
        CreateTeamRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetRequiredUserId();
        var team = await service.CreateTeamAsync(currentUserId, request.GameId, request.Name, cancellationToken);
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
    [Authorize(Policy = "AdminOrSelfRouteUserId")]
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
    [Authorize]
    [EndpointSummary("Add a user to a team.")]
    [ProducesResponseType<TeamDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDto>> AddTeamMember(
        Guid teamId,
        AddTeamMemberRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetRequiredUserId();
        var team = await service.AddMemberAsync(teamId, currentUserId, request.UserId, request.Role, cancellationToken);
        return Ok(team);
    }

    [HttpPut("{teamId:guid}/members/{userId}", Name = "UpdateTeamMemberRole")]
    [Authorize]
    [EndpointSummary("Update a team member role.")]
    [ProducesResponseType<TeamDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDto>> UpdateTeamMemberRole(
        Guid teamId,
        string userId,
        UpdateTeamMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetRequiredUserId();
        var team = await service.UpdateMemberRoleAsync(teamId, currentUserId, userId, request.Role, cancellationToken);
        return Ok(team);
    }

    [HttpPost("{teamId:guid}/members/{userId}/transfer-ownership", Name = "TransferTeamOwnership")]
    [Authorize]
    [EndpointSummary("Transfer team ownership to another member.")]
    [ProducesResponseType<TeamDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDto>> TransferTeamOwnership(
        Guid teamId,
        string userId,
        TransferTeamOwnershipRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetRequiredUserId();
        var team = await service.TransferOwnershipAsync(teamId, currentUserId, userId, cancellationToken);
        return Ok(team);
    }

    [HttpPut("{teamId:guid}/logo", Name = "UpdateTeamLogo")]
    [Authorize]
    [EndpointSummary("Set or clear a team's logo asset.")]
    [ProducesResponseType<TeamDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDto>> UpdateTeamLogo(
        Guid teamId,
        UpdateTeamLogoRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetRequiredUserId();
        var team = await service.UpdateTeamLogoAsync(teamId, currentUserId, request.LogoAssetId, cancellationToken);
        return Ok(team);
    }

    [HttpGet("{teamId:guid}/invite-keys", Name = "GetTeamInviteKeys")]
    [Authorize]
    [EndpointSummary("List invite keys for a team.")]
    [ProducesResponseType<IReadOnlyList<TeamInviteKeyDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeamInviteKeyDto>>> GetTeamInviteKeys(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetRequiredUserId();
        var inviteKeys = await service.GetInviteKeysAsync(teamId, currentUserId, cancellationToken);
        return Ok(inviteKeys);
    }

    [HttpPost("{teamId:guid}/invite-keys", Name = "CreateTeamInviteKey")]
    [Authorize]
    [EndpointSummary("Create an invite key for a team.")]
    [ProducesResponseType<TeamInviteKeyDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamInviteKeyDto>> CreateTeamInviteKey(
        Guid teamId,
        CreateTeamInviteKeyRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetRequiredUserId();
        var inviteKey = await service.CreateInviteKeyAsync(teamId, currentUserId, request.MaxUses, cancellationToken);
        return Ok(inviteKey);
    }

    [HttpDelete("{teamId:guid}/invite-keys/{key}", Name = "RevokeTeamInviteKey")]
    [Authorize]
    [EndpointSummary("Revoke an invite key for a team.")]
    [ProducesResponseType<TeamInviteKeyDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamInviteKeyDto>> RevokeTeamInviteKey(
        Guid teamId,
        string key,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetRequiredUserId();
        var inviteKey = await service.RevokeInviteKeyAsync(teamId, key, currentUserId, cancellationToken);
        return Ok(inviteKey);
    }

    [HttpPost("{teamId:guid}/invite-keys/{key}/accept", Name = "AcceptTeamInviteKey")]
    [Authorize]
    [EndpointSummary("Accept a team invite key as the current user.")]
    [ProducesResponseType<TeamInviteKeyDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamInviteKeyDto>> AcceptTeamInviteKey(
        Guid teamId,
        string key,
        AcceptTeamInviteKeyRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetRequiredUserId();
        var inviteKey = await service.AcceptInviteAsync(teamId, key, currentUserId, cancellationToken);
        return Ok(inviteKey);
    }

    [HttpPut("{teamId:guid}", Name = "UpdateTeam")]
    [Authorize]
    [EndpointSummary("Update editable team details.")]
    [ProducesResponseType<TeamDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDto>> UpdateTeam(
        Guid teamId,
        UpdateTeamRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetRequiredUserId();
        var team = await service.UpdateTeamAsync(teamId, currentUserId, request.Name, request.BannerAssetId, request.PrimaryColor, request.ProfileLink, cancellationToken);
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
    [Authorize]
    [EndpointSummary("Create a guest player profile owned by the team.")]
    [ProducesResponseType<PlayerProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PlayerProfileDto>> CreateGuestPlayerProfile(
        Guid teamId,
        CreateGuestPlayerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetRequiredUserId();
        var profile = await service.CreateGuestPlayerProfileAsync(teamId, currentUserId, request.Name, request.RankRating, request.ProfileLink, cancellationToken);
        return Ok(profile);
    }

    [HttpPut("{teamId:guid}/guest-player-profiles/{playerProfileId:guid}", Name = "UpdateGuestPlayerProfile")]
    [Authorize]
    [EndpointSummary("Update a guest player profile.")]
    [ProducesResponseType<PlayerProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PlayerProfileDto>> UpdateGuestPlayerProfile(
        Guid teamId,
        Guid playerProfileId,
        UpdateGuestPlayerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetRequiredUserId();
        var profile = await service.UpdateGuestPlayerProfileAsync(teamId, playerProfileId, currentUserId, request.Name, request.RankRating, request.ProfileLink, cancellationToken);
        return Ok(profile);
    }

    [HttpDelete("{teamId:guid}/guest-player-profiles/{playerProfileId:guid}", Name = "DeleteGuestPlayerProfile")]
    [Authorize]
    [EndpointSummary("Delete a guest player profile.")]
    [ProducesResponseType<TeamDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDto>> DeleteGuestPlayerProfile(
        Guid teamId,
        Guid playerProfileId,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetRequiredUserId();
        var team = await service.DeleteGuestPlayerProfileAsync(teamId, playerProfileId, currentUserId, cancellationToken);
        return Ok(team);
    }
}

public sealed record CreateTeamRequest(string ActingUserId, string GameId, string Name);
public sealed record AddTeamMemberRequest(string ActingUserId, string UserId, TeamRoleDto Role);
public sealed record UpdateTeamMemberRoleRequest(string ActingUserId, TeamRoleDto Role);
public sealed record TransferTeamOwnershipRequest(string ActingUserId);
public sealed record UpdateTeamRequest(string ActingUserId, string Name, Guid? BannerAssetId, string? PrimaryColor, string? ProfileLink);
public sealed record UpdateTeamLogoRequest(string ActingUserId, Guid? LogoAssetId);
public sealed record CreateTeamInviteKeyRequest(string ActingUserId, int MaxUses = 1);
public sealed record AcceptTeamInviteKeyRequest(string UserId);
public sealed record CreateGuestPlayerProfileRequest(string ActingUserId, string Name, int? RankRating = null, string? ProfileLink = null);
public sealed record UpdateGuestPlayerProfileRequest(string ActingUserId, string Name, int? RankRating = null, string? ProfileLink = null);
