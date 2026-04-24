namespace AkGaming.Tournaments.Contracts.DTOs;

public sealed record RosterPlayerSnapshotDto(
    Guid Id,
    Guid? SourcePlayerProfileId,
    PlayerProfileTypeDto PlayerProfileType,
    string Name,
    string? UserId,
    DateTimeOffset SourcePlayerProfileLastRevisionUtc,
    DateTimeOffset SnapshotCreatedUtc,
    bool IsPotentiallyOutdated);
