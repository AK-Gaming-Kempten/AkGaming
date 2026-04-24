namespace AkGaming.Tournaments.Contracts.DTOs;

public sealed record GameDto(
    string Id,
    string Name,
    Guid? LogoAssetId);
