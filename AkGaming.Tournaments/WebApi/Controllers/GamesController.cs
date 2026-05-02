using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Authorization;
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

    [HttpPost(Name = "CreateGame")]
    [Authorize(Policy = "AdminOnly")]
    [EndpointSummary("Create a supported game.")]
    [ProducesResponseType<GameDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GameDto>> CreateGame(
        CreateGameRequest request,
        CancellationToken cancellationToken)
    {
        var game = await service.CreateGameAsync(request.Id, request.Name, request.LogoAssetId, cancellationToken);
        return Ok(game);
    }

    [HttpPut("{gameId}/logo", Name = "UpdateGameLogo")]
    [Authorize(Policy = "AdminOnly")]
    [EndpointSummary("Set or clear a supported game's logo asset.")]
    [ProducesResponseType<GameDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GameDto>> UpdateGameLogo(
        string gameId,
        UpdateGameLogoRequest request,
        CancellationToken cancellationToken)
    {
        var game = await service.UpdateGameLogoAsync(gameId, request.LogoAssetId, cancellationToken);
        return Ok(game);
    }

    [HttpDelete("{gameId}", Name = "DeleteGame")]
    [Authorize(Policy = "AdminOnly")]
    [EndpointSummary("Delete a supported game.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteGame(string gameId, CancellationToken cancellationToken)
    {
        await service.DeleteGameAsync(gameId, cancellationToken);
        return NoContent();
    }
}

public sealed record CreateGameRequest(string Id, string Name, Guid? LogoAssetId);
public sealed record UpdateGameLogoRequest(Guid? LogoAssetId);
