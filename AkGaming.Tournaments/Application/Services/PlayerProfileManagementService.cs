using AkGaming.Tournaments.Application.Abstractions;
using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Domain.Enums;

namespace AkGaming.Tournaments.Application.Services;

public sealed class PlayerProfileManagementService(
    IGameRepository gameRepository,
    IPlayerProfileRepository playerProfileRepository,
    IUnitOfWork unitOfWork) : IPlayerProfileManagementService
{
    public async Task<IReadOnlyList<PlayerProfileDto>> GetUserProfilesAsync(string userId, CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);

        var profiles = await playerProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        return profiles
            .Where(profile => profile.Type == PlayerProfileType.User)
            .OrderBy(profile => profile.GameId, StringComparer.OrdinalIgnoreCase)
            .Select(profile => profile.ToDto())
            .ToList();
    }

    public async Task<PlayerProfileDto> UpsertUserProfileAsync(string userId, string gameId, string name, CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);
        ValidateName(name, "Player profile");
        await RequireGameAsync(gameId, cancellationToken);

        var trimmedGameId = gameId.Trim();
        var playerProfile = await playerProfileRepository.GetByUserAndGameAsync(userId.Trim(), trimmedGameId, cancellationToken);
        if (playerProfile is null)
        {
            playerProfile = new PlayerProfile
            {
                Id = Guid.NewGuid(),
                GameId = trimmedGameId,
                Name = name.Trim(),
                Type = PlayerProfileType.User,
                UserId = userId.Trim()
            };

            await playerProfileRepository.AddAsync(playerProfile, cancellationToken);
        }
        else
        {
            playerProfile.Name = name.Trim();
            playerProfile.LastRevisionUtc = DateTimeOffset.UtcNow;
            playerProfile.Type = PlayerProfileType.User;
            playerProfile.TeamId = null;
            playerProfile.UserId = userId.Trim();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return playerProfile.ToDto();
    }

    private async Task RequireGameAsync(string gameId, CancellationToken cancellationToken)
    {
        ValidateGameId(gameId);

        if (await gameRepository.GetByIdAsync(gameId.Trim(), cancellationToken) is null)
        {
            throw new NotFoundException($"Game '{gameId}' was not found.");
        }
    }

    private static void ValidateUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ValidationException("User id is required.");
        }
    }

    private static void ValidateGameId(string gameId)
    {
        if (string.IsNullOrWhiteSpace(gameId))
        {
            throw new ValidationException("Game id is required.");
        }
    }

    private static void ValidateName(string name, string subject)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException($"{subject} name is required.");
        }
    }
}
