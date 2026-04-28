using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Tournaments.WebApi.Controllers;

[ApiController]
[Route("api/tournaments")]
[Tags("Tournaments")]
public sealed class TournamentsController(
    ITournamentLogoManagementService logoService,
    ITournamentRegistrationRuleManagementService ruleService) : ControllerBase
{
    [HttpPut("{tournamentId:guid}/logo", Name = "UpdateTournamentLogo")]
    [EndpointSummary("Set or clear a tournament logo asset.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateTournamentLogo(
        Guid tournamentId,
        UpdateTournamentLogoRequest request,
        CancellationToken cancellationToken)
    {
        await logoService.UpdateTournamentLogoAsync(tournamentId, request.LogoAssetId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{tournamentId:guid}/registration-rules", Name = "ReplaceTournamentRegistrationRules")]
    [EndpointSummary("Replace the registration rules for a tournament.")]
    [ProducesResponseType<IReadOnlyList<TournamentRegistrationRuleDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TournamentRegistrationRuleDto>>> ReplaceTournamentRegistrationRules(
        Guid tournamentId,
        ReplaceTournamentRegistrationRulesRequest request,
        CancellationToken cancellationToken)
    {
        var rules = await ruleService.ReplaceRegistrationRulesAsync(tournamentId, request.Rules, cancellationToken);
        return Ok(rules);
    }
}

public sealed record UpdateTournamentLogoRequest(Guid? LogoAssetId);
public sealed record ReplaceTournamentRegistrationRulesRequest(IReadOnlyList<TournamentRegistrationRuleUpdateDto> Rules);
