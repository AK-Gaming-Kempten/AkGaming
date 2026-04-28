using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Frontend.Api;

public sealed class TournamentsApiClient(HttpClient httpClient) : TournamentApiClientBase(httpClient)
{
    public Task<IReadOnlyList<TournamentSummaryDto>> GetTournamentsAsync(CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<TournamentSummaryDto>>("api/tournaments", cancellationToken);

    public Task<TournamentDto?> GetTournamentAsync(string slug, CancellationToken cancellationToken = default)
        => GetOrDefaultAsync<TournamentDto>($"api/tournaments/{Uri.EscapeDataString(slug)}", cancellationToken);

    public Task<TournamentDto> UpdateTournamentContentAsync(
        Guid tournamentId,
        DateTimeOffset? registrationOpenUtc,
        DateTimeOffset? registrationClosedUtc,
        DateTimeOffset? startUtc,
        DateTimeOffset? endUtc,
        IReadOnlyList<TournamentInfoSectionDto> infoSections,
        CancellationToken cancellationToken = default)
        => PutAsync<TournamentDto>(
            $"api/tournaments/{tournamentId}/content",
            new UpdateTournamentContentApiRequest(
                registrationOpenUtc,
                registrationClosedUtc,
                startUtc,
                endUtc,
                infoSections.Select(section => new TournamentInfoSectionUpdateApiRequest(section.Header, section.ContentMarkdown)).ToList()),
            cancellationToken);

    private sealed record UpdateTournamentContentApiRequest(
        DateTimeOffset? RegistrationOpenUtc,
        DateTimeOffset? RegistrationClosedUtc,
        DateTimeOffset? StartUtc,
        DateTimeOffset? EndUtc,
        IReadOnlyList<TournamentInfoSectionUpdateApiRequest> InfoSections);

    private sealed record TournamentInfoSectionUpdateApiRequest(string Header, string ContentMarkdown);
}
