using AkGaming.Tournaments.Application.Abstractions;

namespace AkGaming.Tournaments.WebApi.Endpoints;

public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/games", async (IGameCatalogService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetGamesAsync(cancellationToken)))
            .WithTags("Games")
            .WithName("GetGames")
            .WithSummary("List supported games.");

        return endpoints;
    }
}
