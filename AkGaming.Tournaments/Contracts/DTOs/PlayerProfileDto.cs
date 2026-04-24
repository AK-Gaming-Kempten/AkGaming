namespace AkGaming.Tournaments.Contracts.DTOs;

public sealed record PlayerProfileDto(
    Guid Id,
    string GameId,
    Guid? TeamId,
    PlayerProfileTypeDto Type,
    string Name,
    string? UserId,
    Guid? LogoAssetId,
    DateTimeOffset LastRevisionUtc);
