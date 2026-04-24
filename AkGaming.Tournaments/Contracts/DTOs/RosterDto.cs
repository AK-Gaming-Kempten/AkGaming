namespace AkGaming.Tournaments.Contracts.DTOs;

public sealed record RosterDto(
    Guid Id,
    int Version,
    RosterStatusDto Status,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    string? ReviewNote,
    IReadOnlyList<RosterPlayerSnapshotDto> PlayerSnapshots);
