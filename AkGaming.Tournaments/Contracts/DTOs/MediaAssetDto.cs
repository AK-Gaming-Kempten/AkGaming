namespace AkGaming.Tournaments.Contracts.DTOs;

public sealed record MediaAssetDto(
    Guid Id,
    string Url,
    string ContentType,
    string OriginalFileName,
    long SizeBytes);
