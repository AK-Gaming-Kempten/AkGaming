using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Frontend.Api;

public sealed class TournamentRegistrationsApiClient(HttpClient httpClient) : TournamentApiClientBase(httpClient)
{
    public Task<IReadOnlyList<TournamentRegistrationDto>> GetTournamentRegistrationsAsync(Guid tournamentId, CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<TournamentRegistrationDto>>($"api/tournaments/{tournamentId}/registrations", cancellationToken);

    public Task<IReadOnlyList<TournamentRegistrationDto>> GetTeamRegistrationsAsync(Guid teamId, CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<TournamentRegistrationDto>>($"api/teams/{teamId}/registrations", cancellationToken);

    public Task<TournamentRegistrationDto?> GetRegistrationAsync(Guid registrationId, CancellationToken cancellationToken = default)
        => GetOrDefaultAsync<TournamentRegistrationDto>($"api/registrations/{registrationId}", cancellationToken);

    public Task<TournamentRegistrationEligibilityDto> GetEligibilityAsync(
        Guid teamId,
        Guid tournamentId,
        string actingUserId,
        IReadOnlyCollection<Guid> playerProfileIds,
        CancellationToken cancellationToken = default)
        => PostAsync<TournamentRegistrationEligibilityDto>(
            $"api/teams/{teamId}/registrations/eligibility",
            new TournamentRegistrationEligibilityApiRequest(actingUserId, tournamentId, playerProfileIds),
            cancellationToken);

    public Task<TournamentRegistrationDto> SubmitRegistrationAsync(
        Guid teamId,
        Guid tournamentId,
        string actingUserId,
        IReadOnlyCollection<Guid> playerProfileIds,
        CancellationToken cancellationToken = default)
        => PostAsync<TournamentRegistrationDto>(
            $"api/teams/{teamId}/registrations",
            new SubmitTournamentRegistrationApiRequest(actingUserId, tournamentId, playerProfileIds),
            cancellationToken);

    public Task<TournamentRegistrationDto> SubmitRosterChangeAsync(
        Guid registrationId,
        string actingUserId,
        IReadOnlyCollection<Guid> playerProfileIds,
        CancellationToken cancellationToken = default)
        => PostAsync<TournamentRegistrationDto>(
            $"api/registrations/{registrationId}/rosters",
            new SubmitRosterChangeApiRequest(actingUserId, playerProfileIds),
            cancellationToken);

    public Task<TournamentRegistrationDto> ReviewRegistrationAsync(
        Guid registrationId,
        bool approve,
        string? reviewNote,
        CancellationToken cancellationToken = default)
        => PostAsync<TournamentRegistrationDto>(
            $"api/registrations/{registrationId}/review",
            new ReviewRegistrationApiRequest(approve, reviewNote),
            cancellationToken);

    public Task<TournamentRegistrationDto> ReviewRosterChangeAsync(
        Guid registrationId,
        Guid rosterId,
        bool approve,
        string? reviewNote,
        CancellationToken cancellationToken = default)
        => PostAsync<TournamentRegistrationDto>(
            $"api/registrations/{registrationId}/rosters/{rosterId}/review",
            new ReviewRegistrationApiRequest(approve, reviewNote),
            cancellationToken);

    public Task DeleteRegistrationAsync(Guid registrationId, CancellationToken cancellationToken = default)
        => DeleteAsync($"api/registrations/{registrationId}", cancellationToken);

    private sealed record SubmitTournamentRegistrationApiRequest(string ActingUserId, Guid TournamentId, IReadOnlyCollection<Guid> PlayerProfileIds);
    private sealed record TournamentRegistrationEligibilityApiRequest(string ActingUserId, Guid TournamentId, IReadOnlyCollection<Guid> PlayerProfileIds);
    private sealed record SubmitRosterChangeApiRequest(string ActingUserId, IReadOnlyCollection<Guid> PlayerProfileIds);
    private sealed record ReviewRegistrationApiRequest(bool Approve, string? ReviewNote);
}
