using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Tournaments.WebApi.Controllers;

[ApiController]
[Route("api/games")]
[Tags("Games")]
public sealed class GamesController(IGameCatalogService service) : ControllerBase
{
    [HttpGet(Name = "GetGames")]
    [EndpointSummary("List supported games.")]
    [ProducesResponseType<IReadOnlyList<GameDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GameDto>>> GetGames(CancellationToken cancellationToken)
    {
        var games = await service.GetGamesAsync(cancellationToken);
        return Ok(games);
    }
}
