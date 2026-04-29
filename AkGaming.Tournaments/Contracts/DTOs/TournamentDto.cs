namespace AkGaming.Tournaments.Contracts.DTOs;

public sealed record TournamentDto(
    Guid Id,
    string Slug,
    string GameId,
    string GameName,
    string Name,
    Guid? LogoAssetId,
    TournamentStatusDto Status,
    DateTimeOffset? RegistrationOpenUtc,
    DateTimeOffset? RegistrationClosedUtc,
    DateTimeOffset? StartUtc,
    DateTimeOffset? EndUtc,
    IReadOnlyList<TournamentInfoSectionDto> InfoSections,
    IReadOnlyList<TournamentRegistrationRuleDto> RegistrationRules);
