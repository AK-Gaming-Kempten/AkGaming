using AkGaming.Tournaments.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Tournaments.WebApi.Controllers;

[ApiController]
[Route("api/tournaments")]
[Tags("Tournaments")]
public sealed class TournamentsController(ITournamentLogoManagementService service) : ControllerBase
{
    [HttpPut("{tournamentId:guid}/logo", Name = "UpdateTournamentLogo")]
    [EndpointSummary("Set or clear a tournament logo asset.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateTournamentLogo(
        Guid tournamentId,
        UpdateTournamentLogoRequest request,
        CancellationToken cancellationToken)
    {
        await service.UpdateTournamentLogoAsync(tournamentId, request.LogoAssetId, cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateTournamentLogoRequest(Guid? LogoAssetId);
