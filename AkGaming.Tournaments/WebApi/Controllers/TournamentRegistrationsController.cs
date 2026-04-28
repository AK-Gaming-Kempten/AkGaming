using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Tournaments.WebApi.Controllers;

[ApiController]
[Tags("Tournament Registrations")]
public sealed class TournamentRegistrationsController(ITournamentRegistrationService service) : ControllerBase
{
    [HttpGet("api/tournaments/{tournamentId:guid}/registrations", Name = "GetTournamentRegistrations")]
    [EndpointSummary("List tournament registrations for a tournament.")]
    [ProducesResponseType<IReadOnlyList<TournamentRegistrationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TournamentRegistrationDto>>> GetTournamentRegistrations(
        Guid tournamentId,
        CancellationToken cancellationToken)
    {
        var registrations = await service.GetTournamentRegistrationsAsync(tournamentId, cancellationToken);
        return Ok(registrations);
    }

    [HttpGet("api/teams/{teamId:guid}/registrations", Name = "GetTeamRegistrations")]
    [EndpointSummary("List tournament registrations for a team.")]
    [ProducesResponseType<IReadOnlyList<TournamentRegistrationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TournamentRegistrationDto>>> GetTeamRegistrations(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var registrations = await service.GetTeamRegistrationsAsync(teamId, cancellationToken);
        return Ok(registrations);
    }

    [HttpPost("api/teams/{teamId:guid}/registrations/eligibility", Name = "GetTournamentRegistrationEligibility")]
    [EndpointSummary("Preview whether a team roster can register for a tournament.")]
    [ProducesResponseType<TournamentRegistrationEligibilityDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TournamentRegistrationEligibilityDto>> GetTournamentRegistrationEligibility(
        Guid teamId,
        TournamentRegistrationEligibilityRequest request,
        CancellationToken cancellationToken)
    {
        var eligibility = await service.GetRegistrationEligibilityAsync(
            teamId,
            request.TournamentId,
            request.ActingUserId,
            request.PlayerProfileIds,
            cancellationToken);

        return Ok(eligibility);
    }

    [HttpPost("api/teams/{teamId:guid}/registrations", Name = "SubmitTournamentRegistration")]
    [EndpointSummary("Submit an initial tournament registration.")]
    [ProducesResponseType<TournamentRegistrationDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TournamentRegistrationDto>> SubmitTournamentRegistration(
        Guid teamId,
        SubmitTournamentRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var registration = await service.SubmitRegistrationAsync(
            teamId,
            request.TournamentId,
            request.ActingUserId,
            request.PlayerProfileIds,
            cancellationToken);

        return Ok(registration);
    }

    [HttpGet("api/registrations/{registrationId:guid}", Name = "GetTournamentRegistration")]
    [EndpointSummary("Get a tournament registration.")]
    [ProducesResponseType<TournamentRegistrationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TournamentRegistrationDto>> GetTournamentRegistration(
        Guid registrationId,
        CancellationToken cancellationToken)
    {
        var registration = await service.GetRegistrationAsync(registrationId, cancellationToken);
        return registration is null ? NotFound() : Ok(registration);
    }

    [HttpPost("api/registrations/{registrationId:guid}/review", Name = "ReviewTournamentRegistration")]
    [EndpointSummary("Approve or reject a pending tournament registration.")]
    [ProducesResponseType<TournamentRegistrationDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TournamentRegistrationDto>> ReviewTournamentRegistration(
        Guid registrationId,
        ReviewRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var registration = await service.ReviewRegistrationAsync(
            registrationId,
            request.Approve,
            request.ReviewNote,
            cancellationToken);

        return Ok(registration);
    }

    [HttpPost("api/registrations/{registrationId:guid}/rosters", Name = "SubmitRosterChange")]
    [EndpointSummary("Submit a roster change for an approved registration.")]
    [ProducesResponseType<TournamentRegistrationDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TournamentRegistrationDto>> SubmitRosterChange(
        Guid registrationId,
        SubmitRosterChangeRequest request,
        CancellationToken cancellationToken)
    {
        var registration = await service.SubmitRosterChangeAsync(
            registrationId,
            request.ActingUserId,
            request.PlayerProfileIds,
            cancellationToken);

        return Ok(registration);
    }

    [HttpPost("api/registrations/{registrationId:guid}/rosters/{rosterId:guid}/review", Name = "ReviewRosterChange")]
    [EndpointSummary("Approve or reject a pending roster change.")]
    [ProducesResponseType<TournamentRegistrationDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TournamentRegistrationDto>> ReviewRosterChange(
        Guid registrationId,
        Guid rosterId,
        ReviewRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var registration = await service.ReviewRosterAsync(
            registrationId,
            rosterId,
            request.Approve,
            request.ReviewNote,
            cancellationToken);

        return Ok(registration);
    }
}

public sealed record SubmitTournamentRegistrationRequest(string ActingUserId, Guid TournamentId, IReadOnlyCollection<Guid> PlayerProfileIds);
public sealed record TournamentRegistrationEligibilityRequest(string ActingUserId, Guid TournamentId, IReadOnlyCollection<Guid> PlayerProfileIds);
public sealed record SubmitRosterChangeRequest(string ActingUserId, IReadOnlyCollection<Guid> PlayerProfileIds);
public sealed record ReviewRegistrationRequest(bool Approve, string? ReviewNote);
