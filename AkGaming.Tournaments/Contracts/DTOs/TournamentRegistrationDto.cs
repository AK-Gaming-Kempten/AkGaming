namespace AkGaming.Tournaments.Contracts.DTOs;

public sealed record TournamentRegistrationDto(
    Guid Id,
    Guid TournamentId,
    Guid TeamId,
    TournamentRegistrationStatusDto Status,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    string? ReviewNote,
    Guid? ActiveRosterId,
    IReadOnlyList<RosterDto> Rosters);
