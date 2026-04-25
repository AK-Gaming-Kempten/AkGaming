using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Domain.Entities;

namespace AkGaming.Tournaments.Application.Services;

public sealed class GameCatalogService(
    IGameRepository gameRepository,
    IUnitOfWork unitOfWork) : IGameCatalogService
{
    public async Task<IReadOnlyList<GameDto>> GetGamesAsync(CancellationToken cancellationToken = default)
    {
        var games = await gameRepository.GetAllAsync(cancellationToken);
        return games
            .OrderBy(game => game.Name, StringComparer.OrdinalIgnoreCase)
            .Select(game => game.ToDto())
            .ToList();
    }

    public async Task<GameDto> CreateGameAsync(string gameId, string name, Guid? logoAssetId, CancellationToken cancellationToken = default)
    {
        var normalizedGameId = NormalizeGameId(gameId);
        var normalizedName = NormalizeName(name);
        await RequireLogoAssetAsync(logoAssetId, cancellationToken);

        if (await gameRepository.GetByIdAsync(normalizedGameId, cancellationToken) is not null)
        {
            throw new ConflictException($"Game '{normalizedGameId}' already exists.");
        }

        var game = new Game
        {
            Id = normalizedGameId,
            Name = normalizedName,
            LogoAssetId = logoAssetId
        };

        await gameRepository.AddAsync(game, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return game.ToDto();
    }

    public async Task<GameDto> UpdateGameLogoAsync(string gameId, Guid? logoAssetId, CancellationToken cancellationToken = default)
    {
        var normalizedGameId = NormalizeGameId(gameId);
        await RequireLogoAssetAsync(logoAssetId, cancellationToken);

        var game = await RequireGameAsync(normalizedGameId, cancellationToken);
        game.LogoAssetId = logoAssetId;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return game.ToDto();
    }

    public async Task DeleteGameAsync(string gameId, CancellationToken cancellationToken = default)
    {
        var normalizedGameId = NormalizeGameId(gameId);
        var game = await RequireGameAsync(normalizedGameId, cancellationToken);

        if (await gameRepository.IsGameInUseAsync(normalizedGameId, cancellationToken))
        {
            throw new ConflictException("Games that are used by teams, player profiles, or tournaments cannot be deleted.");
        }

        gameRepository.Delete(game);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Game> RequireGameAsync(string gameId, CancellationToken cancellationToken)
    {
        return await gameRepository.GetByIdAsync(gameId, cancellationToken)
               ?? throw new NotFoundException($"Game '{gameId}' was not found.");
    }

    private async Task RequireLogoAssetAsync(Guid? logoAssetId, CancellationToken cancellationToken)
    {
        if (logoAssetId is not Guid assetId)
            return;

        if (!await gameRepository.MediaAssetExistsAsync(assetId, cancellationToken))
        {
            throw new NotFoundException($"Media asset '{assetId}' was not found.");
        }
    }

    private static string NormalizeGameId(string gameId)
    {
        if (string.IsNullOrWhiteSpace(gameId))
        {
            throw new ValidationException("Game id is required.");
        }

        return gameId.Trim();
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Game name is required.");
        }

        return name.Trim();
    }
}
