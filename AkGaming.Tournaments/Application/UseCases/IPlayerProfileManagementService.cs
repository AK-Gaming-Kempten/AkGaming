using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Application.UseCases;

public interface IPlayerProfileManagementService
{
    Task<IReadOnlyList<PlayerProfileDto>> GetUserProfilesAsync(string userId, CancellationToken cancellationToken = default);
    Task<PlayerProfileDto> UpsertUserProfileAsync(string userId, string gameId, string name, CancellationToken cancellationToken = default);
}
