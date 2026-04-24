using AkGaming.Tournaments.Application.Abstractions;

namespace AkGaming.Tournaments.WebApi.Endpoints;

public static class PlayerProfileEndpoints
{
    public static IEndpointRouteBuilder MapPlayerProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users/{userId}/player-profiles")
            .WithTags("Player Profiles");

        group.MapGet("/", async (string userId, IPlayerProfileManagementService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetUserProfilesAsync(userId, cancellationToken)))
            .WithName("GetUserPlayerProfiles")
            .WithSummary("List player profiles for a user.");

        group.MapPut("/{gameId}", async (string userId, string gameId, UpsertUserPlayerProfileRequest request, IPlayerProfileManagementService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.UpsertUserProfileAsync(userId, gameId, request.Name, cancellationToken)))
            .WithName("UpsertUserPlayerProfile")
            .WithSummary("Create or update a user player profile for a game.");

        return endpoints;
    }

    public sealed record UpsertUserPlayerProfileRequest(string Name);
}
