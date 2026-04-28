using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Domain.Entities;

namespace AkGaming.Tournaments.Tests.Fakes;

internal sealed class InMemoryStore
{
    public List<Game> Games { get; } = [];
    public List<MediaAsset> MediaAssets { get; } = [];
    public List<PlayerProfile> PlayerProfiles { get; } = [];
    public List<Team> Teams { get; } = [];
    public List<Tournament> Tournaments { get; } = [];
    public List<TournamentRegistration> Registrations { get; } = [];
}

internal sealed class InMemoryGameRepository(InMemoryStore store) : IGameRepository
{
    public Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Game>>(store.Games.OrderBy(game => game.Name).ToList());

    public Task<Game?> GetByIdAsync(string gameId, CancellationToken cancellationToken = default)
        => Task.FromResult(store.Games.FirstOrDefault(game => game.Id == gameId));

    public Task<bool> IsGameInUseAsync(string gameId, CancellationToken cancellationToken = default)
        => Task.FromResult(
            store.Teams.Any(team => team.GameId == gameId)
            || store.PlayerProfiles.Any(profile => profile.GameId == gameId)
            || store.Tournaments.Any(tournament => tournament.GameId == gameId));

    public Task<bool> MediaAssetExistsAsync(Guid mediaAssetId, CancellationToken cancellationToken = default)
        => Task.FromResult(store.MediaAssets.Any(mediaAsset => mediaAsset.Id == mediaAssetId));

    public Task AddAsync(Game game, CancellationToken cancellationToken = default)
    {
        store.Games.Add(game);
        return Task.CompletedTask;
    }

    public void Delete(Game game)
        => store.Games.Remove(game);
}

internal sealed class InMemoryMediaAssetRepository(InMemoryStore store) : IMediaAssetRepository
{
    public Task<MediaAsset?> GetByIdAsync(Guid mediaAssetId, CancellationToken cancellationToken = default)
        => Task.FromResult(store.MediaAssets.FirstOrDefault(mediaAsset => mediaAsset.Id == mediaAssetId));

    public Task AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default)
    {
        store.MediaAssets.Add(mediaAsset);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryPlayerProfileRepository(InMemoryStore store) : IPlayerProfileRepository
{
    public Task<PlayerProfile?> GetByIdAsync(Guid playerProfileId, CancellationToken cancellationToken = default)
        => Task.FromResult(store.PlayerProfiles.FirstOrDefault(playerProfile => playerProfile.Id == playerProfileId));

    public Task<IReadOnlyList<PlayerProfile>> GetByIdsAsync(IEnumerable<Guid> playerProfileIds, CancellationToken cancellationToken = default)
    {
        var ids = playerProfileIds.Distinct().ToHashSet();
        return Task.FromResult<IReadOnlyList<PlayerProfile>>(store.PlayerProfiles.Where(playerProfile => ids.Contains(playerProfile.Id)).ToList());
    }

    public Task<IReadOnlyList<PlayerProfile>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PlayerProfile>>(store.PlayerProfiles.Where(playerProfile => playerProfile.UserId == userId).ToList());

    public Task<PlayerProfile?> GetByUserAndGameAsync(string userId, string gameId, CancellationToken cancellationToken = default)
        => Task.FromResult(store.PlayerProfiles.FirstOrDefault(playerProfile => playerProfile.UserId == userId && playerProfile.GameId == gameId));

    public Task<IReadOnlyList<PlayerProfile>> GetByUsersAndGameAsync(IEnumerable<string> userIds, string gameId, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Task.FromResult<IReadOnlyList<PlayerProfile>>(store.PlayerProfiles
            .Where(playerProfile => playerProfile.GameId == gameId
                                    && playerProfile.UserId is not null
                                    && ids.Contains(playerProfile.UserId))
            .ToList());
    }

    public Task AddAsync(PlayerProfile playerProfile, CancellationToken cancellationToken = default)
    {
        store.PlayerProfiles.Add(playerProfile);
        return Task.CompletedTask;
    }

    public void Delete(PlayerProfile playerProfile)
    {
        store.PlayerProfiles.Remove(playerProfile);
        if (playerProfile.TeamId is Guid teamId)
        {
            store.Teams.FirstOrDefault(team => team.Id == teamId)?.GuestPlayerProfiles.Remove(playerProfile);
        }
    }
}

internal sealed class InMemoryTeamRepository(InMemoryStore store) : ITeamRepository
{
    public Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken = default)
        => Task.FromResult(store.Teams.FirstOrDefault(team => team.Id == teamId));

    public Task<IReadOnlyList<Team>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Team>>(store.Teams
            .Where(team => team.Memberships.Any(member => member.UserId == userId))
            .OrderBy(team => team.Name)
            .ToList());

    public Task AddAsync(Team team, CancellationToken cancellationToken = default)
    {
        store.Teams.Add(team);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryTournamentRepository(InMemoryStore store) : ITournamentRepository
{
    public Task<Tournament?> GetByIdAsync(Guid tournamentId, CancellationToken cancellationToken = default)
        => Task.FromResult(store.Tournaments.FirstOrDefault(tournament => tournament.Id == tournamentId));
}

internal sealed class InMemoryTournamentRegistrationRepository(InMemoryStore store) : ITournamentRegistrationRepository
{
    public Task<TournamentRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken = default)
        => Task.FromResult(store.Registrations.FirstOrDefault(registration => registration.Id == registrationId));

    public Task<IReadOnlyList<TournamentRegistration>> GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TournamentRegistration>>(store.Registrations.Where(registration => registration.TeamId == teamId).ToList());

    public Task<IReadOnlyList<TournamentRegistration>> GetByTournamentIdAsync(Guid tournamentId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TournamentRegistration>>(store.Registrations.Where(registration => registration.TournamentId == tournamentId).ToList());

    public Task<TournamentRegistration?> GetByTeamAndTournamentAsync(Guid teamId, Guid tournamentId, CancellationToken cancellationToken = default)
        => Task.FromResult(store.Registrations.FirstOrDefault(registration => registration.TeamId == teamId && registration.TournamentId == tournamentId));

    public Task AddAsync(TournamentRegistration registration, CancellationToken cancellationToken = default)
    {
        registration.Team ??= store.Teams.FirstOrDefault(team => team.Id == registration.TeamId);
        registration.Tournament ??= store.Tournaments.FirstOrDefault(tournament => tournament.Id == registration.TournamentId);
        store.Registrations.Add(registration);
        registration.Team?.Registrations.Add(registration);
        return Task.CompletedTask;
    }
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
