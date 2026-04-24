namespace AkGaming.Tournaments.Contracts.DTOs;

public sealed record TeamMembershipDto(
    string UserId,
    TeamRoleDto Role,
    DateTimeOffset JoinedAtUtc);
