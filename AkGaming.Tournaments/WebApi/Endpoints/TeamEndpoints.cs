using AkGaming.Tournaments.Application.Abstractions;
using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.WebApi.Endpoints;

public static class TeamEndpoints
{
    public static IEndpointRouteBuilder MapTeamEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/teams")
            .WithTags("Teams");

        group.MapPost("/", async (CreateTeamRequest request, ITeamManagementService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.CreateTeamAsync(request.ActingUserId, request.Name, cancellationToken)))
            .WithName("CreateTeam")
            .WithSummary("Create a team and assign the creator as owner.");

        group.MapGet("/{teamId:guid}", async (Guid teamId, ITeamManagementService service, CancellationToken cancellationToken) =>
        {
            var team = await service.GetTeamAsync(teamId, cancellationToken);
            return team is null ? Results.NotFound() : Results.Ok(team);
        })
            .WithName("GetTeam")
            .WithSummary("Get a team with memberships and guest profiles.");

        group.MapPost("/{teamId:guid}/members", async (Guid teamId, AddTeamMemberRequest request, ITeamManagementService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.AddMemberAsync(teamId, request.ActingUserId, request.UserId, request.Role, cancellationToken)))
            .WithName("AddTeamMember")
            .WithSummary("Add a user to a team.");

        group.MapPut("/{teamId:guid}/members/{userId}", async (Guid teamId, string userId, UpdateTeamMemberRoleRequest request, ITeamManagementService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdateMemberRoleAsync(teamId, request.ActingUserId, userId, request.Role, cancellationToken)))
            .WithName("UpdateTeamMemberRole")
            .WithSummary("Update a team member role.");

        group.MapGet("/{teamId:guid}/available-player-profiles/{gameId}", async (Guid teamId, string gameId, ITeamManagementService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAvailableProfilesAsync(teamId, gameId, cancellationToken)))
            .WithName("GetAvailableTeamProfiles")
            .WithSummary("List all player profiles the team can use for a game.");

        group.MapPost("/{teamId:guid}/guest-player-profiles", async (Guid teamId, CreateGuestPlayerProfileRequest request, ITeamManagementService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.CreateGuestPlayerProfileAsync(teamId, request.ActingUserId, request.GameId, request.Name, cancellationToken)))
            .WithName("CreateGuestPlayerProfile")
            .WithSummary("Create a guest player profile owned by the team.");

        group.MapPut("/{teamId:guid}/guest-player-profiles/{playerProfileId:guid}", async (Guid teamId, Guid playerProfileId, UpdateGuestPlayerProfileRequest request, ITeamManagementService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdateGuestPlayerProfileAsync(teamId, playerProfileId, request.ActingUserId, request.Name, cancellationToken)))
            .WithName("UpdateGuestPlayerProfile")
            .WithSummary("Update a guest player profile.");

        return endpoints;
    }

    public sealed record CreateTeamRequest(string ActingUserId, string Name);
    public sealed record AddTeamMemberRequest(string ActingUserId, string UserId, TeamRoleDto Role);
    public sealed record UpdateTeamMemberRoleRequest(string ActingUserId, TeamRoleDto Role);
    public sealed record CreateGuestPlayerProfileRequest(string ActingUserId, string GameId, string Name);
    public sealed record UpdateGuestPlayerProfileRequest(string ActingUserId, string Name);
}
