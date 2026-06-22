using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Tournaments.WebApi.Controllers;

[ApiController]
[Route("api/tournaments")]
[Tags("Tournaments")]
public sealed class TournamentsController(
    ITournamentCatalogService catalogService,
    ITournamentAdministrationService administrationService,
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

    [HttpGet("admin", Name = "GetAdminTournaments")]
    [Authorize(Policy = "tournaments.tournaments.manage")]
    [EndpointSummary("Get all tournaments including hidden ones for administration.")]
    [ProducesResponseType<IReadOnlyList<TournamentSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TournamentSummaryDto>>> GetAdminTournaments(CancellationToken cancellationToken)
    {
        var tournaments = await administrationService.GetTournamentsAsync(cancellationToken);
        return Ok(tournaments);
    }

    [HttpGet("admin/by-slug/{slug}", Name = "GetAdminTournamentBySlug")]
    [Authorize(Policy = "tournaments.tournaments.manage")]
    [EndpointSummary("Get a tournament by slug for administration, including hidden tournaments.")]
    [ProducesResponseType<TournamentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TournamentDto>> GetAdminTournamentBySlug(string slug, CancellationToken cancellationToken)
    {
        var tournament = await administrationService.GetTournamentBySlugAsync(slug, cancellationToken);
        if (tournament is null)
        {
            return NotFound();
        }

        return Ok(tournament);
    }

    [HttpPost("admin", Name = "CreateTournament")]
    [Authorize(Policy = "tournaments.tournaments.manage")]
    [EndpointSummary("Create a tournament.")]
    [ProducesResponseType<TournamentDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TournamentDto>> CreateTournament(CreateTournamentRequest request, CancellationToken cancellationToken)
    {
        var tournament = await administrationService.CreateTournamentAsync(
            request.Slug,
            request.GameId,
            request.Name,
            request.IsVisible,
            cancellationToken);
        return Ok(tournament);
    }

    [HttpPut("{tournamentId:guid}/visibility", Name = "UpdateTournamentVisibility")]
    [Authorize(Policy = "tournaments.tournaments.manage")]
    [EndpointSummary("Set the public visibility of a tournament.")]
    [ProducesResponseType<TournamentDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TournamentDto>> UpdateTournamentVisibility(
        Guid tournamentId,
        UpdateTournamentVisibilityRequest request,
        CancellationToken cancellationToken)
    {
        var tournament = await administrationService.UpdateTournamentVisibilityAsync(tournamentId, request.IsVisible, cancellationToken);
        return Ok(tournament);
    }

    [HttpDelete("{tournamentId:guid}", Name = "DeleteTournament")]
    [Authorize(Policy = "tournaments.tournaments.manage")]
    [EndpointSummary("Delete a tournament.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTournament(Guid tournamentId, CancellationToken cancellationToken)
    {
        await administrationService.DeleteTournamentAsync(tournamentId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{tournamentId:guid}/logo", Name = "UpdateTournamentLogo")]
    [Authorize(Policy = "tournaments.tournaments.manage")]
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
    [Authorize(Policy = "tournaments.tournaments.manage")]
    [EndpointSummary("Replace tournament timeline fields and info sections.")]
    [ProducesResponseType<TournamentDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TournamentDto>> UpdateTournamentContent(
        Guid tournamentId,
        UpdateTournamentContentRequest request,
        CancellationToken cancellationToken)
    {
        var tournament = await contentService.UpdateTournamentContentAsync(
            tournamentId,
            request.Name,
            request.Status,
            request.BannerAssetId,
            request.PrimaryColor,
            request.RegistrationOpenUtc,
            request.RegistrationClosedUtc,
            request.StartUtc,
            request.EndUtc,
            request.InfoSections,
            cancellationToken);
        return Ok(tournament);
    }

    [HttpPut("{tournamentId:guid}/registration-rules", Name = "ReplaceTournamentRegistrationRules")]
    [Authorize(Policy = "tournaments.tournaments.manage")]
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
public sealed record CreateTournamentRequest(string Slug, string GameId, string Name, bool IsVisible);
public sealed record UpdateTournamentVisibilityRequest(bool IsVisible);
public sealed record UpdateTournamentContentRequest(
    string Name,
    TournamentStatusDto Status,
    Guid? BannerAssetId,
    string? PrimaryColor,
    DateTimeOffset? RegistrationOpenUtc,
    DateTimeOffset? RegistrationClosedUtc,
    DateTimeOffset? StartUtc,
    DateTimeOffset? EndUtc,
    IReadOnlyList<TournamentInfoSectionUpdateDto> InfoSections);
public sealed record ReplaceTournamentRegistrationRulesRequest(IReadOnlyList<TournamentRegistrationRuleUpdateDto> Rules);
