using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Frontend.Api;

public sealed class TournamentsApiClient(HttpClient httpClient) : TournamentApiClientBase(httpClient)
{
    public Task<IReadOnlyList<TournamentSummaryDto>> GetTournamentsAsync(CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<TournamentSummaryDto>>("api/tournaments", cancellationToken, authorize: false);

    public Task<IReadOnlyList<TournamentSummaryDto>> GetAdminTournamentsAsync(CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<TournamentSummaryDto>>("api/tournaments/admin", cancellationToken);

    public Task<TournamentDto?> GetTournamentAsync(string slug, bool includeHidden = false, CancellationToken cancellationToken = default)
    {
        var encodedSlug = Uri.EscapeDataString(slug);
        var uri = includeHidden
            ? $"api/tournaments/admin/by-slug/{encodedSlug}"
            : $"api/tournaments/{encodedSlug}";
        return GetOrDefaultAsync<TournamentDto>(uri, cancellationToken, authorize: includeHidden);
    }

    public Task<TournamentDto> CreateTournamentAsync(
        string slug,
        string gameId,
        string name,
        bool isVisible,
        CancellationToken cancellationToken = default)
        => PostAsync<TournamentDto>(
            "api/tournaments/admin",
            new CreateTournamentApiRequest(slug, gameId, name, isVisible),
            cancellationToken);

    public async Task UpdateTournamentLogoAsync(Guid tournamentId, Guid? logoAssetId, CancellationToken cancellationToken = default)
    {
        await PutAsync(
            $"api/tournaments/{tournamentId}/logo",
            new UpdateTournamentLogoApiRequest(logoAssetId),
            cancellationToken);
    }

    public Task<TournamentDto> UpdateTournamentVisibilityAsync(Guid tournamentId, bool isVisible, CancellationToken cancellationToken = default)
        => PutAsync<TournamentDto>(
            $"api/tournaments/{tournamentId}/visibility",
            new UpdateTournamentVisibilityApiRequest(isVisible),
            cancellationToken);

    public Task<TournamentDto> UpdateTournamentContentAsync(
        Guid tournamentId,
        string name,
        TournamentStatusDto status,
        Guid? bannerAssetId,
        string? primaryColor,
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
                bannerAssetId,
                primaryColor,
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

    public Task DeleteTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default)
        => DeleteAsync($"api/tournaments/{tournamentId}", cancellationToken);

    private sealed record CreateTournamentApiRequest(string Slug, string GameId, string Name, bool IsVisible);
    private sealed record UpdateTournamentLogoApiRequest(Guid? LogoAssetId);
    private sealed record UpdateTournamentVisibilityApiRequest(bool IsVisible);
    private sealed record ReplaceTournamentRegistrationRulesApiRequest(IReadOnlyList<TournamentRegistrationRuleUpdateRequest> Rules);

    private sealed record UpdateTournamentContentApiRequest(
        string Name,
        TournamentStatusDto Status,
        Guid? BannerAssetId,
        string? PrimaryColor,
        DateTimeOffset? RegistrationOpenUtc,
        DateTimeOffset? RegistrationClosedUtc,
        DateTimeOffset? StartUtc,
        DateTimeOffset? EndUtc,
        IReadOnlyList<TournamentInfoSectionUpdateApiRequest> InfoSections);

    private sealed record TournamentInfoSectionUpdateApiRequest(string Header, string ContentMarkdown);
}

public sealed record TournamentRegistrationRuleUpdateRequest(string Type, int Value);
