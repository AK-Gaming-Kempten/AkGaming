using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Domain.Enums;
using AkGaming.Tournaments.Tests.Fakes;

namespace AkGaming.Tournaments.Tests.Application;

public sealed class PlayerProfileManagementServiceTests
{

    private InMemoryStore Store { get; set; } = null!;

    private FakeUnitOfWork UnitOfWork { get; set; } = null!;

    private PlayerProfileManagementService Service { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Store = new InMemoryStore();
        UnitOfWork = new FakeUnitOfWork();
        Service = new PlayerProfileManagementService(
            new InMemoryGameRepository(Store),
            new InMemoryPlayerProfileRepository(Store),
            UnitOfWork);
    }


    [Test]
    [Description("Verifies that listing a user's profiles returns only user-backed profiles and orders them by game id.")]
    public void GetUserProfilesAsync_FiltersGuestProfilesAndOrdersByGame()
    {
        TournamentTestData.AddUserProfile(Store, "user-1", TournamentTestData.OtherGameId, "Duelist");
        TournamentTestData.AddUserProfile(Store, "user-1", TournamentTestData.GameId, "Summoner");
        var team = TournamentTestData.AddTeam(Store);
        TournamentTestData.AddGuestProfile(Store, team, "Guest");

        var profiles = Service.GetUserProfilesAsync("user-1").GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(profiles, Has.Count.EqualTo(2));
            Assert.That(profiles.Select(profile => profile.GameId), Is.EqualTo(new[] { TournamentTestData.GameId, TournamentTestData.OtherGameId }));
            Assert.That(profiles.All(profile => profile.Type == PlayerProfileTypeDto.User), Is.True);
        });
    }

    [Test]
    [Description("Verifies that listing profiles requires a non-empty user id.")]
    public void GetUserProfilesAsync_RejectsBlankUserId()
    {
        Assert.ThrowsAsync<ValidationException>(() => Service.GetUserProfilesAsync(" "));
    }

    [Test]
    [Description("Verifies that user-backed player profiles are scoped to one profile per user and game, and that upsert updates the existing profile.")]
    public void UpsertUserProfileAsync_CreatesAndThenUpdatesSingleProfilePerGame()
    {
        TournamentTestData.AddGame(Store);

        var created = Service.UpsertUserProfileAsync("user-1", " lol ", " Summoner One ").GetAwaiter().GetResult();
        var updated = Service.UpsertUserProfileAsync(" user-1 ", "lol", "Summoner Prime").GetAwaiter().GetResult();

        Assert.That(Store.PlayerProfiles, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(created.Type, Is.EqualTo(PlayerProfileTypeDto.User));
            Assert.That(created.GameId, Is.EqualTo(TournamentTestData.GameId));
            Assert.That(created.Name, Is.EqualTo("Summoner One"));
            Assert.That(updated.Id, Is.EqualTo(created.Id));
            Assert.That(updated.Name, Is.EqualTo("Summoner Prime"));
            Assert.That(Store.PlayerProfiles[0].Type, Is.EqualTo(PlayerProfileType.User));
            Assert.That(Store.PlayerProfiles[0].TeamId, Is.Null);
            Assert.That(UnitOfWork.SaveChangesCallCount, Is.EqualTo(2));
        });
    }

    [Test]
    [Description("Verifies that player profiles for the same user can exist independently for different games.")]
    public void UpsertUserProfileAsync_CreatesSeparateProfilesForDifferentGames()
    {
        TournamentTestData.AddGame(Store);
        TournamentTestData.AddGame(Store, TournamentTestData.OtherGameId, "Valorant");

        var lol = Service.UpsertUserProfileAsync("user-1", TournamentTestData.GameId, "Summoner").GetAwaiter().GetResult();
        var valorant = Service.UpsertUserProfileAsync("user-1", TournamentTestData.OtherGameId, "Duelist").GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(Store.PlayerProfiles, Has.Count.EqualTo(2));
            Assert.That(valorant.Id, Is.Not.EqualTo(lol.Id));
            Assert.That(Store.PlayerProfiles.Select(profile => profile.GameId), Is.EquivalentTo(new[] { TournamentTestData.GameId, TournamentTestData.OtherGameId }));
        });
    }

    [Test]
    [Description("Verifies that user profile commands require a non-empty user id, game id, and profile name.")]
    public void UpsertUserProfileAsync_RejectsRequiredBlankFields()
    {
        TournamentTestData.AddGame(Store);

        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<ValidationException>(() => Service.UpsertUserProfileAsync(" ", TournamentTestData.GameId, "Summoner"));
            Assert.ThrowsAsync<ValidationException>(() => Service.UpsertUserProfileAsync("user-1", " ", "Summoner"));
            Assert.ThrowsAsync<ValidationException>(() => Service.UpsertUserProfileAsync("user-1", TournamentTestData.GameId, " "));
        });
    }

    [Test]
    [Description("Verifies that a user profile cannot be created for an unknown game.")]
    public void UpsertUserProfileAsync_RejectsUnknownGame()
    {
        Assert.ThrowsAsync<NotFoundException>(() =>
            Service.UpsertUserProfileAsync("user-1", TournamentTestData.GameId, "Summoner"));
    }
}
