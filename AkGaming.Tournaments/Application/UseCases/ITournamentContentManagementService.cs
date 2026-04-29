using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Application.UseCases;

public interface ITournamentContentManagementService
{
    Task<TournamentDto> UpdateTournamentContentAsync(
        Guid tournamentId,
        string name,
        TournamentStatusDto status,
        Guid? bannerAssetId,
        string? primaryColor,
        DateTimeOffset? registrationOpenUtc,
        DateTimeOffset? registrationClosedUtc,
        DateTimeOffset? startUtc,
        DateTimeOffset? endUtc,
        IReadOnlyList<TournamentInfoSectionUpdateDto> infoSections,
        CancellationToken cancellationToken = default);
}

public sealed record TournamentInfoSectionUpdateDto(string Header, string ContentMarkdown);
