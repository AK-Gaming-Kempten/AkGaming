namespace AkGaming.Tournaments.Contracts.DTOs;

public sealed record TeamDto(
    Guid Id,
    string Name,
    Guid? LogoAssetId,
    IReadOnlyList<TeamMembershipDto> Memberships,
    IReadOnlyList<PlayerProfileDto> GuestPlayerProfiles);
