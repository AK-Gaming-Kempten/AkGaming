using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Domain.Entities;

namespace AkGaming.Tournaments.Tests.Fakes;

internal sealed class InMemoryStore
{
    public List<Game> Games { get; } = [];
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
}

internal sealed class InMemoryTeamRepository(InMemoryStore store) : ITeamRepository
{
    public Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken = default)
        => Task.FromResult(store.Teams.FirstOrDefault(team => team.Id == teamId));

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
