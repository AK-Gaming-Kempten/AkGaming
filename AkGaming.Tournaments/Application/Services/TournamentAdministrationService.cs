using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Domain.Enums;

namespace AkGaming.Tournaments.Application.Services;

public sealed class TournamentAdministrationService(
    ITournamentRepository tournamentRepository,
    IGameRepository gameRepository,
    IUnitOfWork unitOfWork) : ITournamentAdministrationService
{
    public async Task<IReadOnlyList<TournamentSummaryDto>> GetTournamentsAsync(CancellationToken cancellationToken = default)
    {
        var tournaments = await tournamentRepository.GetAllAsync(includeHidden: true, cancellationToken);
        return tournaments
            .OrderBy(tournament => tournament.Name, StringComparer.OrdinalIgnoreCase)
            .Select(tournament => tournament.ToSummaryDto())
            .ToList();
    }

    public async Task<TournamentDto?> GetTournamentBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var tournament = await tournamentRepository.GetBySlugAsync(slug.Trim(), includeHidden: true, cancellationToken);
        return tournament?.ToDto();
    }

    public async Task<TournamentDto> CreateTournamentAsync(
        string slug,
        string gameId,
        string name,
        bool isVisible,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = NormalizeSlug(slug);
        var normalizedGameId = NormalizeGameId(gameId);
        var normalizedName = NormalizeName(name);

        if (await tournamentRepository.GetBySlugAsync(normalizedSlug, includeHidden: true, cancellationToken) is not null)
        {
            throw new ConflictException($"Tournament slug '{normalizedSlug}' already exists.");
        }

        if (await gameRepository.GetByIdAsync(normalizedGameId, cancellationToken) is null)
        {
            throw new NotFoundException($"Game '{normalizedGameId}' was not found.");
        }

        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Slug = normalizedSlug,
            GameId = normalizedGameId,
            Name = normalizedName,
            IsVisible = isVisible,
            Status = TournamentStatus.Draft
        };

        await tournamentRepository.AddAsync(tournament, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        tournament.Game = await gameRepository.GetByIdAsync(normalizedGameId, cancellationToken);
        return tournament.ToDto();
    }

    public async Task<TournamentDto> UpdateTournamentVisibilityAsync(Guid tournamentId, bool isVisible, CancellationToken cancellationToken = default)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId, cancellationToken)
                         ?? throw new NotFoundException($"Tournament '{tournamentId}' was not found.");

        tournament.IsVisible = isVisible;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return tournament.ToDto();
    }

    public async Task DeleteTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId, cancellationToken)
                         ?? throw new NotFoundException($"Tournament '{tournamentId}' was not found.");

        if (tournament.Registrations.Count > 0)
        {
            throw new ConflictException("Tournaments with existing registrations cannot be deleted.");
        }

        tournamentRepository.Delete(tournament);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ValidationException("Tournament slug is required.");
        }

        return slug.Trim().ToLowerInvariant();
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
            throw new ValidationException("Tournament name is required.");
        }

        return name.Trim();
    }
}
