namespace AkGaming.Tournaments.Contracts.DTOs;

public sealed record TeamDto(
    Guid Id,
    string GameId,
    string Name,
    Guid? LogoAssetId,
    Guid? BannerAssetId,
    string? PrimaryColor,
    IReadOnlyList<TeamMembershipDto> Memberships,
    IReadOnlyList<PlayerProfileDto> GuestPlayerProfiles);
