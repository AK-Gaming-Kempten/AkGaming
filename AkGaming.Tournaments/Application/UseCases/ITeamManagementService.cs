using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Application.UseCases;

public interface ITeamManagementService
{
    Task<TeamDto?> GetTeamAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamDto>> GetTeamsForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<TeamDto> CreateTeamAsync(string actingUserId, string gameId, string name, CancellationToken cancellationToken = default);
    Task<TeamDto> AddMemberAsync(Guid teamId, string actingUserId, string userId, TeamRoleDto role, CancellationToken cancellationToken = default);
    Task<TeamDto> UpdateMemberRoleAsync(Guid teamId, string actingUserId, string userId, TeamRoleDto role, CancellationToken cancellationToken = default);
    Task<TeamDto> UpdateTeamLogoAsync(Guid teamId, string actingUserId, Guid? logoAssetId, CancellationToken cancellationToken = default);
    Task<PlayerProfileDto> CreateGuestPlayerProfileAsync(Guid teamId, string actingUserId, string name, CancellationToken cancellationToken = default);
    Task<PlayerProfileDto> UpdateGuestPlayerProfileAsync(Guid teamId, Guid playerProfileId, string actingUserId, string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlayerProfileDto>> GetAvailableProfilesAsync(Guid teamId, string gameId, CancellationToken cancellationToken = default);
}
