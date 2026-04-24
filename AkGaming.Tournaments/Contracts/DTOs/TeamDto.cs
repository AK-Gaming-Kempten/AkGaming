namespace AkGaming.Tournaments.Contracts.DTOs;

public sealed record TeamDto(
    Guid Id,
    string GameId,
    string Name,
    Guid? LogoAssetId,
    IReadOnlyList<TeamMembershipDto> Memberships,
    IReadOnlyList<PlayerProfileDto> GuestPlayerProfiles);
