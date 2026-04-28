using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Application.UseCases;

public interface IPlayerProfileManagementService
{
    Task<IReadOnlyList<PlayerProfileDto>> GetUserProfilesAsync(string userId, CancellationToken cancellationToken = default);
    Task<PlayerProfileDto> UpsertUserProfileAsync(string userId, string gameId, string name, int? rankRating = null, CancellationToken cancellationToken = default);
    Task<PlayerProfileDto> UpdateUserProfileLogoAsync(string userId, string gameId, Guid? logoAssetId, CancellationToken cancellationToken = default);
}
