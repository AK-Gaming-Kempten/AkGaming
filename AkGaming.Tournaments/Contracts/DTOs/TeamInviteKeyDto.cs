namespace AkGaming.Tournaments.Contracts.DTOs;

public sealed record TeamInviteKeyDto(
    Guid Id,
    Guid TeamId,
    string Key,
    int RemainingUses,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? RevokedUtc);
