namespace AkGaming.Tournaments.Contracts.DTOs;

public sealed record PlayerProfileDto(
    Guid Id,
    string GameId,
    Guid? TeamId,
    PlayerProfileTypeDto Type,
    string Name,
    int? RankRating,
    string? UserId,
    string? ProfileLink,
    Guid? LogoAssetId,
    DateTimeOffset LastRevisionUtc);
