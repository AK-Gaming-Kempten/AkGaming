using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Frontend.Api;

public sealed class TournamentsApiClient(HttpClient httpClient) : TournamentApiClientBase(httpClient)
{
    public Task<IReadOnlyList<TournamentSummaryDto>> GetTournamentsAsync(CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<TournamentSummaryDto>>("api/tournaments", cancellationToken);

    public Task<TournamentDto?> GetTournamentAsync(string slug, CancellationToken cancellationToken = default)
        => GetOrDefaultAsync<TournamentDto>($"api/tournaments/{Uri.EscapeDataString(slug)}", cancellationToken);

    public async Task UpdateTournamentLogoAsync(Guid tournamentId, Guid? logoAssetId, CancellationToken cancellationToken = default)
    {
        await PutAsync(
            $"api/tournaments/{tournamentId}/logo",
            new UpdateTournamentLogoApiRequest(logoAssetId),
            cancellationToken);
    }

    public Task<TournamentDto> UpdateTournamentContentAsync(
        Guid tournamentId,
        string name,
        TournamentStatusDto status,
        DateTimeOffset? registrationOpenUtc,
        DateTimeOffset? registrationClosedUtc,
        DateTimeOffset? startUtc,
        DateTimeOffset? endUtc,
        IReadOnlyList<TournamentInfoSectionDto> infoSections,
        CancellationToken cancellationToken = default)
        => PutAsync<TournamentDto>(
            $"api/tournaments/{tournamentId}/content",
            new UpdateTournamentContentApiRequest(
                name,
                status,
                registrationOpenUtc,
                registrationClosedUtc,
                startUtc,
                endUtc,
                infoSections.Select(section => new TournamentInfoSectionUpdateApiRequest(section.Header, section.ContentMarkdown)).ToList()),
            cancellationToken);

    public Task<IReadOnlyList<TournamentRegistrationRuleDto>> ReplaceTournamentRegistrationRulesAsync(
        Guid tournamentId,
        IReadOnlyList<TournamentRegistrationRuleUpdateRequest> rules,
        CancellationToken cancellationToken = default)
        => PutAsync<IReadOnlyList<TournamentRegistrationRuleDto>>(
            $"api/tournaments/{tournamentId}/registration-rules",
            new ReplaceTournamentRegistrationRulesApiRequest(rules),
            cancellationToken);

    private sealed record UpdateTournamentLogoApiRequest(Guid? LogoAssetId);
    private sealed record ReplaceTournamentRegistrationRulesApiRequest(IReadOnlyList<TournamentRegistrationRuleUpdateRequest> Rules);

    private sealed record UpdateTournamentContentApiRequest(
        string Name,
        TournamentStatusDto Status,
        DateTimeOffset? RegistrationOpenUtc,
        DateTimeOffset? RegistrationClosedUtc,
        DateTimeOffset? StartUtc,
        DateTimeOffset? EndUtc,
        IReadOnlyList<TournamentInfoSectionUpdateApiRequest> InfoSections);

    private sealed record TournamentInfoSectionUpdateApiRequest(string Header, string ContentMarkdown);
}

public sealed record TournamentRegistrationRuleUpdateRequest(string Type, int Value);
