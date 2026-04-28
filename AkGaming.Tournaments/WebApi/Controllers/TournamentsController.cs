using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Tournaments.WebApi.Controllers;

[ApiController]
[Route("api/tournaments")]
[Tags("Tournaments")]
public sealed class TournamentsController(
    ITournamentCatalogService catalogService,
    ITournamentContentManagementService contentService,
    ITournamentLogoManagementService logoService,
    ITournamentRegistrationRuleManagementService ruleService) : ControllerBase
{
    [HttpGet(Name = "GetTournaments")]
    [EndpointSummary("Get all tournaments.")]
    [ProducesResponseType<IReadOnlyList<TournamentSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TournamentSummaryDto>>> GetTournaments(CancellationToken cancellationToken)
    {
        var tournaments = await catalogService.GetTournamentsAsync(cancellationToken);
        return Ok(tournaments);
    }

    [HttpGet("{slug}", Name = "GetTournamentBySlug")]
    [EndpointSummary("Get a tournament by slug.")]
    [ProducesResponseType<TournamentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TournamentDto>> GetTournamentBySlug(string slug, CancellationToken cancellationToken)
    {
        var tournament = await catalogService.GetTournamentBySlugAsync(slug, cancellationToken);
        if (tournament is null)
        {
            return NotFound();
        }

        return Ok(tournament);
    }

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

    [HttpPut("{tournamentId:guid}/content", Name = "UpdateTournamentContent")]
    [EndpointSummary("Replace tournament timeline fields and info sections.")]
    [ProducesResponseType<TournamentDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TournamentDto>> UpdateTournamentContent(
        Guid tournamentId,
        UpdateTournamentContentRequest request,
        CancellationToken cancellationToken)
    {
        var tournament = await contentService.UpdateTournamentContentAsync(
            tournamentId,
            request.RegistrationOpenUtc,
            request.RegistrationClosedUtc,
            request.StartUtc,
            request.EndUtc,
            request.InfoSections,
            cancellationToken);
        return Ok(tournament);
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
public sealed record UpdateTournamentContentRequest(
    DateTimeOffset? RegistrationOpenUtc,
    DateTimeOffset? RegistrationClosedUtc,
    DateTimeOffset? StartUtc,
    DateTimeOffset? EndUtc,
    IReadOnlyList<TournamentInfoSectionUpdateDto> InfoSections);
public sealed record ReplaceTournamentRegistrationRulesRequest(IReadOnlyList<TournamentRegistrationRuleUpdateDto> Rules);
