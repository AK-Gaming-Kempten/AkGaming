namespace AkGaming.Tournaments.Contracts.DTOs;

public sealed record TournamentRegistrationEligibilityDto(
    Guid TournamentId,
    Guid TeamId,
    bool CanSubmit,
    bool CanEditTeam,
    string? ExistingRegistrationStatus,
    IReadOnlyList<TournamentRegistrationRuleDto> Rules,
    IReadOnlyList<TournamentRegistrationPlayerEligibilityDto> Players,
    IReadOnlyList<TournamentRegistrationRuleCheckDto> Checks);

public sealed record TournamentRegistrationRuleDto(
    string Type,
    string Label,
    int Value,
    string DisplayValue);

public sealed record TournamentRegistrationPlayerEligibilityDto(
    Guid PlayerProfileId,
    string Name,
    PlayerProfileTypeDto Type,
    string? UserId,
    int? RankRating,
    string RankLabel,
    string? RankName,
    string? RankDivision,
    int? RankPoints,
    bool Selected,
    bool Qualifies,
    IReadOnlyList<string> Reasons);

public sealed record TournamentRegistrationRuleCheckDto(
    string Label,
    string Description,
    bool Passed,
    string Tone);
