using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Domain.Enums;

namespace AkGaming.Tournaments.Application.Services;

public sealed class PlayerProfileManagementService(
    IGameRepository gameRepository,
    IMediaAssetRepository mediaAssetRepository,
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

    public async Task<PlayerProfileDto> UpsertUserProfileAsync(string userId, string gameId, string name, int? rankRating = null, string? profileLink = null, CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);
        ValidateName(name, "Player profile");
        await RequireGameAsync(gameId, cancellationToken);
        var normalizedProfileLink = NormalizeHttpsLink(profileLink, "Player profile link");

        var trimmedGameId = gameId.Trim();
        var playerProfile = await playerProfileRepository.GetByUserAndGameAsync(userId.Trim(), trimmedGameId, cancellationToken);
        if (playerProfile is null)
        {
            playerProfile = new PlayerProfile
            {
                Id = Guid.NewGuid(),
                GameId = trimmedGameId,
                Name = name.Trim(),
                RankRating = NormalizeRankRating(rankRating),
                ProfileLink = normalizedProfileLink,
                Type = PlayerProfileType.User,
                UserId = userId.Trim()
            };

            await playerProfileRepository.AddAsync(playerProfile, cancellationToken);
        }
        else
        {
            playerProfile.Name = name.Trim();
            playerProfile.RankRating = NormalizeRankRating(rankRating);
            playerProfile.ProfileLink = normalizedProfileLink;
            playerProfile.LastRevisionUtc = DateTimeOffset.UtcNow;
            playerProfile.Type = PlayerProfileType.User;
            playerProfile.TeamId = null;
            playerProfile.UserId = userId.Trim();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return playerProfile.ToDto();
    }

    public async Task<PlayerProfileDto> UpdateUserProfileLogoAsync(string userId, string gameId, Guid? logoAssetId, CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);
        await RequireGameAsync(gameId, cancellationToken);
        await RequireMediaAssetAsync(logoAssetId, cancellationToken);

        var profile = await playerProfileRepository.GetByUserAndGameAsync(userId.Trim(), gameId.Trim(), cancellationToken)
                      ?? throw new NotFoundException($"Player profile for game '{gameId}' was not found.");

        profile.LogoAssetId = logoAssetId;
        profile.LastRevisionUtc = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return profile.ToDto();
    }

    private async Task RequireGameAsync(string gameId, CancellationToken cancellationToken)
    {
        ValidateGameId(gameId);

        if (await gameRepository.GetByIdAsync(gameId.Trim(), cancellationToken) is null)
        {
            throw new NotFoundException($"Game '{gameId}' was not found.");
        }
    }

    private async Task RequireMediaAssetAsync(Guid? mediaAssetId, CancellationToken cancellationToken)
    {
        if (mediaAssetId is not Guid assetId)
            return;

        if (await mediaAssetRepository.GetByIdAsync(assetId, cancellationToken) is null)
        {
            throw new NotFoundException($"Media asset '{assetId}' was not found.");
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

    private static int? NormalizeRankRating(int? rankRating)
        => rankRating.HasValue ? Math.Max(0, rankRating.Value) : null;

    private static string? NormalizeHttpsLink(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
        {
            return uri.ToString();
        }

        throw new ValidationException($"{fieldName} must be a valid https URL.");
    }
}
