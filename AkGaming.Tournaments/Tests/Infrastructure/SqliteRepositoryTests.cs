using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Domain.Enums;
using AkGaming.Tournaments.Infrastructure.Sqlite.Persistence;
using AkGaming.Tournaments.Infrastructure.Sqlite.Repositories;
using AkGaming.Tournaments.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Tournaments.Tests.Infrastructure;

public sealed class SqliteRepositoryTests
{

    private SqliteTestDatabase Database { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Database = new SqliteTestDatabase();
    }

    [TearDown]
    public void TearDown()
    {
        Database.Dispose();
    }


    [Test]
    [Description("Verifies that the SQLite EF unit of work persists repository additions.")]
    public async Task EfUnitOfWork_SaveChangesAsync_PersistsRepositoryAdditions()
    {
        await SeedGamesAsync();
        var team = new Team
        {
            Id = Guid.NewGuid(),
            GameId = TournamentTestData.GameId,
            Name = "AKG Blue",
            Memberships =
            [
                new TeamMembership
                {
                    Id = Guid.NewGuid(),
                    UserId = TournamentTestData.OwnerId,
                    Role = TeamRole.Owner
                }
            ]
        };

        await using (var dbContext = Database.CreateContext())
        {
            var repository = new TeamRepository(dbContext);
            var unitOfWork = new EfUnitOfWork(dbContext);

            await repository.AddAsync(team);
            await unitOfWork.SaveChangesAsync();
        }

        await using var queryContext = Database.CreateContext();
        var saved = await queryContext.Teams.Include(item => item.Memberships).SingleAsync(item => item.Id == team.Id);

        Assert.Multiple(() =>
        {
            Assert.That(saved.Name, Is.EqualTo("AKG Blue"));
            Assert.That(saved.Memberships.Single().Role, Is.EqualTo(TeamRole.Owner));
        });
    }

    [Test]
    [Description("Verifies that the SQLite game repository returns games ordered by name and can look up a game by id.")]
    public async Task GameRepository_ReturnsOrderedGamesAndFindsById()
    {
        await using (var dbContext = Database.CreateContext())
        {
            dbContext.Games.AddRange(
                new Game { Id = "valorant", Name = "Valorant" },
                new Game { Id = "lol", Name = "League of Legends" },
                new Game { Id = "cs2", Name = "Counter-Strike 2" });
            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = Database.CreateContext();
        var repository = new GameRepository(queryContext);

        var games = await repository.GetAllAsync();
        var game = await repository.GetByIdAsync("lol");

        Assert.Multiple(() =>
        {
            Assert.That(games.Select(item => item.Name), Is.EqualTo(new[] { "Counter-Strike 2", "League of Legends", "Valorant" }));
            Assert.That(game, Is.Not.Null);
            Assert.That(game!.Name, Is.EqualTo("League of Legends"));
        });
    }

    [Test]
    [Description("Verifies that the SQLite player profile repository resolves distinct ids and returns an empty result for an empty id set.")]
    public async Task PlayerProfileRepository_GetByIdsAsync_HandlesDistinctAndEmptyIdSets()
    {
        await SeedGamesAsync();
        PlayerProfile first;
        PlayerProfile second;
        await using (var dbContext = Database.CreateContext())
        {
            first = UserProfile("user-1", "lol", "Summoner");
            second = UserProfile("user-2", "lol", "Support");
            dbContext.PlayerProfiles.AddRange(first, second);
            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = Database.CreateContext();
        var repository = new PlayerProfileRepository(queryContext);

        var profiles = await repository.GetByIdsAsync([first.Id, first.Id, second.Id]);
        var empty = await repository.GetByIdsAsync([]);

        Assert.Multiple(() =>
        {
            Assert.That(profiles.Select(profile => profile.Id), Is.EquivalentTo(new[] { first.Id, second.Id }));
            Assert.That(empty, Is.Empty);
        });
    }

    [Test]
    [Description("Verifies that the SQLite player profile repository returns user profiles ordered by game and name.")]
    public async Task PlayerProfileRepository_GetByUserIdAsync_ReturnsProfilesOrderedByGameAndName()
    {
        await SeedGamesAsync();
        await using (var dbContext = Database.CreateContext())
        {
            dbContext.PlayerProfiles.AddRange(
                UserProfile("user-1", "valorant", "Duelist"),
                UserProfile("user-1", "lol", "Summoner B"),
                UserProfile("user-1", "lol", "Summoner A"),
                UserProfile("user-2", "lol", "Other User"));
            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = Database.CreateContext();
        var repository = new PlayerProfileRepository(queryContext);

        var profiles = await repository.GetByUserIdAsync("user-1");

        Assert.That(
            profiles.Select(profile => (profile.GameId, profile.Name)),
            Is.EqualTo(new[] { ("lol", "Summoner A"), ("lol", "Summoner B"), ("valorant", "Duelist") }));
    }

    [Test]
    [Description("Verifies that the SQLite player profile repository filters available user profiles by users and game.")]
    public async Task PlayerProfileRepository_GetByUsersAndGameAsync_FiltersByUsersAndGame()
    {
        await SeedGamesAsync();
        await using (var dbContext = Database.CreateContext())
        {
            dbContext.PlayerProfiles.AddRange(
                UserProfile("captain-1", "lol", "Captain Top"),
                UserProfile("member-1", "lol", "Member Jungle"),
                UserProfile("member-1", "valorant", "Wrong Game"),
                UserProfile("other-user", "lol", "Wrong User"));
            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = Database.CreateContext();
        var repository = new PlayerProfileRepository(queryContext);

        var profiles = await repository.GetByUsersAndGameAsync(["captain-1", "member-1", "CAPTAIN-1", " "], "lol");
        var empty = await repository.GetByUsersAndGameAsync([], "lol");

        Assert.Multiple(() =>
        {
            Assert.That(profiles.Select(profile => profile.Name), Is.EqualTo(new[] { "Captain Top", "Member Jungle" }));
            Assert.That(empty, Is.Empty);
        });
    }

    [Test]
    [Description("Verifies that the SQLite team repository loads memberships and guest profiles needed by application team rules.")]
    public async Task TeamRepository_GetByIdAsync_LoadsMembershipsAndGuestProfiles()
    {
        var team = await SeedTeamWithProfilesAsync();

        await using var queryContext = Database.CreateContext();
        var repository = new TeamRepository(queryContext);

        var loaded = await repository.GetByIdAsync(team.Id);

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.Memberships.Select(membership => membership.UserId), Is.EquivalentTo(new[] { TournamentTestData.OwnerId, TournamentTestData.MemberId }));
            Assert.That(loaded.GuestPlayerProfiles.Select(profile => profile.Name), Is.EqualTo(new[] { "Guest Support" }));
        });
    }

    [Test]
    [Description("Verifies that the SQLite model enforces one registration per team and tournament.")]
    public async Task TournamentRegistrationModel_EnforcesUniqueTeamTournamentRegistration()
    {
        var team = await SeedTeamWithProfilesAsync();
        await using var dbContext = Database.CreateContext();
        var tournament = Tournament("Campus Clash");
        dbContext.Tournaments.Add(tournament);
        await dbContext.SaveChangesAsync();

        dbContext.TournamentRegistrations.Add(Registration(team.Id, tournament.Id, DateTimeOffset.UtcNow));
        dbContext.TournamentRegistrations.Add(Registration(team.Id, tournament.Id, DateTimeOffset.UtcNow.AddMinutes(1)));

        Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [Test]
    [Description("Verifies that the SQLite tournament registration repository loads the aggregate graph needed for registration and roster workflows.")]
    public async Task TournamentRegistrationRepository_GetByIdAsync_LoadsTeamTournamentActiveRosterAndSnapshots()
    {
        var registration = await SeedApprovedRegistrationAsync();

        await using var queryContext = Database.CreateContext();
        var repository = new TournamentRegistrationRepository(queryContext);

        var loaded = await repository.GetByIdAsync(registration.Id);

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.Team, Is.Not.Null);
            Assert.That(loaded.Team!.Memberships, Has.Count.EqualTo(2));
            Assert.That(loaded.Team.GuestPlayerProfiles, Has.Count.EqualTo(1));
            Assert.That(loaded.Tournament, Is.Not.Null);
            Assert.That(loaded.ActiveRoster, Is.Not.Null);
            Assert.That(loaded.Rosters.Single().PlayerSnapshots, Has.Count.EqualTo(2));
        });
    }

    [Test]
    [Description("Verifies that the SQLite tournament registration repository returns a team's registrations newest first.")]
    public async Task TournamentRegistrationRepository_GetByTeamIdAsync_ReturnsNewestFirst()
    {
        var team = await SeedTeamWithProfilesAsync();
        TournamentRegistration older;
        TournamentRegistration newer;
        await using (var dbContext = Database.CreateContext())
        {
            var tournamentOne = Tournament("Campus Clash 1");
            var tournamentTwo = Tournament("Campus Clash 2");
            dbContext.Tournaments.AddRange(tournamentOne, tournamentTwo);
            await dbContext.SaveChangesAsync();

            older = Registration(team.Id, tournamentOne.Id, DateTimeOffset.UtcNow.AddDays(-1));
            newer = Registration(team.Id, tournamentTwo.Id, DateTimeOffset.UtcNow);
            dbContext.TournamentRegistrations.AddRange(older, newer);
            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = Database.CreateContext();
        var repository = new TournamentRegistrationRepository(queryContext);

        var registrations = await repository.GetByTeamIdAsync(team.Id);

        Assert.That(registrations.Select(registration => registration.Id), Is.EqualTo(new[] { newer.Id, older.Id }));
    }

    private async Task SeedGamesAsync()
    {
        await using var dbContext = Database.CreateContext();
        dbContext.Games.AddRange(
            new Game { Id = TournamentTestData.GameId, Name = "League of Legends" },
            new Game { Id = TournamentTestData.OtherGameId, Name = "Valorant" });
        await dbContext.SaveChangesAsync();
    }


    private async Task<Team> SeedTeamWithProfilesAsync()
    {
        await SeedGamesAsync();
        var team = new Team
        {
            Id = Guid.NewGuid(),
            GameId = TournamentTestData.GameId,
            Name = "AKG Blue",
            Memberships =
            [
                new TeamMembership { Id = Guid.NewGuid(), UserId = TournamentTestData.OwnerId, Role = TeamRole.Owner },
                new TeamMembership { Id = Guid.NewGuid(), UserId = TournamentTestData.MemberId, Role = TeamRole.Member }
            ],
            GuestPlayerProfiles =
            [
                new PlayerProfile
                {
                    Id = Guid.NewGuid(),
                    GameId = TournamentTestData.GameId,
                    Name = "Guest Support",
                    Type = PlayerProfileType.Guest
                }
            ]
        };
        team.GuestPlayerProfiles.Single().TeamId = team.Id;

        await using var dbContext = Database.CreateContext();
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();
        return team;
    }


    private async Task<TournamentRegistration> SeedApprovedRegistrationAsync()
    {
        var team = await SeedTeamWithProfilesAsync();
        PlayerProfile memberProfile;
        await using (var dbContext = Database.CreateContext())
        {
            memberProfile = UserProfile(TournamentTestData.OwnerId, TournamentTestData.GameId, "Captain Top");
            dbContext.PlayerProfiles.Add(memberProfile);
            var tournament = Tournament("Campus Clash");
            dbContext.Tournaments.Add(tournament);
            await dbContext.SaveChangesAsync();

            var roster = new Roster
            {
                Id = Guid.NewGuid(),
                Version = 1,
                Status = RosterStatus.Approved,
                PlayerSnapshots =
                [
                    Snapshot(memberProfile),
                    Snapshot(team.GuestPlayerProfiles.Single())
                ]
            };
            var registration = new TournamentRegistration
            {
                Id = Guid.NewGuid(),
                TeamId = team.Id,
                TournamentId = tournament.Id,
                Status = TournamentRegistrationStatus.Approved,
                Rosters = [roster]
            };
            roster.TournamentRegistrationId = registration.Id;

            dbContext.TournamentRegistrations.Add(registration);
            await dbContext.SaveChangesAsync();

            registration.ActiveRosterId = roster.Id;
            registration.ActiveRoster = roster;
            await dbContext.SaveChangesAsync();
            return registration;
        }
    }


    private static PlayerProfile UserProfile(string userId, string gameId, string name)
        => new()
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Name = name,
            Type = PlayerProfileType.User,
            UserId = userId
        };


    private static Tournament Tournament(string name)
        => new()
        {
            Id = Guid.NewGuid(),
            GameId = TournamentTestData.GameId,
            Name = name,
            Status = TournamentStatus.RegistrationOpen
        };


    private static TournamentRegistration Registration(Guid teamId, Guid tournamentId, DateTimeOffset submittedAtUtc)
        => new()
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            TournamentId = tournamentId,
            Status = TournamentRegistrationStatus.Pending,
            SubmittedAtUtc = submittedAtUtc
        };


    private static RosterPlayerSnapshot Snapshot(PlayerProfile profile)
        => new()
        {
            Id = Guid.NewGuid(),
            SourcePlayerProfileId = profile.Id,
            PlayerProfileType = profile.Type,
            Name = profile.Name,
            UserId = profile.UserId,
            SourcePlayerProfileLastRevisionUtc = profile.LastRevisionUtc,
            SnapshotCreatedUtc = DateTimeOffset.UtcNow
        };
}
