using AkGaming.Tournaments.Application.Abstractions;

namespace AkGaming.Tournaments.WebApi.Endpoints;

public static class TournamentRegistrationEndpoints
{
    public static IEndpointRouteBuilder MapTournamentRegistrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/teams/{teamId:guid}/registrations", async (Guid teamId, ITournamentRegistrationService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetTeamRegistrationsAsync(teamId, cancellationToken)))
            .WithTags("Tournament Registrations")
            .WithName("GetTeamRegistrations")
            .WithSummary("List tournament registrations for a team.");

        endpoints.MapGet("/api/registrations/{registrationId:guid}", async (Guid registrationId, ITournamentRegistrationService service, CancellationToken cancellationToken) =>
        {
            var registration = await service.GetRegistrationAsync(registrationId, cancellationToken);
            return registration is null ? Results.NotFound() : Results.Ok(registration);
        })
            .WithTags("Tournament Registrations")
            .WithName("GetTournamentRegistration")
            .WithSummary("Get a tournament registration.");

        endpoints.MapPost("/api/teams/{teamId:guid}/registrations", async (Guid teamId, SubmitTournamentRegistrationRequest request, ITournamentRegistrationService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.SubmitRegistrationAsync(teamId, request.TournamentId, request.ActingUserId, request.PlayerProfileIds, cancellationToken)))
            .WithTags("Tournament Registrations")
            .WithName("SubmitTournamentRegistration")
            .WithSummary("Submit an initial tournament registration.");

        endpoints.MapPost("/api/registrations/{registrationId:guid}/review", async (Guid registrationId, ReviewRegistrationRequest request, ITournamentRegistrationService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.ReviewRegistrationAsync(registrationId, request.Approve, request.ReviewNote, cancellationToken)))
            .WithTags("Tournament Registrations")
            .WithName("ReviewTournamentRegistration")
            .WithSummary("Approve or reject a pending tournament registration.");

        endpoints.MapPost("/api/registrations/{registrationId:guid}/rosters", async (Guid registrationId, SubmitRosterChangeRequest request, ITournamentRegistrationService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.SubmitRosterChangeAsync(registrationId, request.ActingUserId, request.PlayerProfileIds, cancellationToken)))
            .WithTags("Tournament Registrations")
            .WithName("SubmitRosterChange")
            .WithSummary("Submit a roster change for an approved registration.");

        endpoints.MapPost("/api/registrations/{registrationId:guid}/rosters/{rosterId:guid}/review", async (Guid registrationId, Guid rosterId, ReviewRegistrationRequest request, ITournamentRegistrationService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.ReviewRosterAsync(registrationId, rosterId, request.Approve, request.ReviewNote, cancellationToken)))
            .WithTags("Tournament Registrations")
            .WithName("ReviewRosterChange")
            .WithSummary("Approve or reject a pending roster change.");

        return endpoints;
    }

    public sealed record SubmitTournamentRegistrationRequest(string ActingUserId, Guid TournamentId, IReadOnlyCollection<Guid> PlayerProfileIds);
    public sealed record SubmitRosterChangeRequest(string ActingUserId, IReadOnlyCollection<Guid> PlayerProfileIds);
    public sealed record ReviewRegistrationRequest(bool Approve, string? ReviewNote);
}
