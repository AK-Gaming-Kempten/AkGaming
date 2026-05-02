namespace AkGaming.Tournaments.Contracts.DTOs;

public sealed record TournamentSummaryDto(
    Guid Id,
    string Slug,
    string GameId,
    string GameName,
    string Name,
    bool IsVisible,
    Guid? LogoAssetId,
    Guid? BannerAssetId,
    string? PrimaryColor,
    TournamentStatusDto Status,
    DateTimeOffset? RegistrationOpenUtc,
    DateTimeOffset? RegistrationClosedUtc,
    DateTimeOffset? StartUtc,
    DateTimeOffset? EndUtc,
    int RegisteredTeamCount);
