using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Domain.Enums;
using AkGaming.Tournaments.Tests.Fakes;

namespace AkGaming.Tournaments.Tests.Application;

public sealed class GameCatalogServiceTests
{

    private InMemoryStore Store { get; set; } = null!;

    private GameCatalogService Service { get; set; } = null!;

    private FakeUnitOfWork UnitOfWork { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Store = new InMemoryStore();
        UnitOfWork = new FakeUnitOfWork();
        Service = new GameCatalogService(new InMemoryGameRepository(Store), UnitOfWork);
    }


    [Test]
    [Description("Verifies that the game catalog returns public game DTOs ordered by display name.")]
    public void GetGamesAsync_ReturnsGamesOrderedByName()
    {
        // Arrange
        TournamentTestData.AddGame(Store, "valorant", "Valorant");
        TournamentTestData.AddGame(Store, "lol", "League of Legends");
        TournamentTestData.AddGame(Store, "cs2", "Counter-Strike 2");

        // Act
        var games = Service.GetGamesAsync().GetAwaiter().GetResult();

        // Assert
        Assert.That(games.Select(game => game.Name), Is.EqualTo(new[] { "Counter-Strike 2", "League of Legends", "Valorant" }));
    }

    [Test]
    [Description("Verifies that admins can create a game with trimmed values and an existing logo asset.")]
    public void CreateGameAsync_AddsGame()
    {
        // Arrange
        var asset = TournamentTestData.AddMediaAsset(Store);

        // Act
        var game = Service.CreateGameAsync(" cs2 ", " Counter-Strike 2 ", asset.Id).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(game.Id, Is.EqualTo("cs2"));
            Assert.That(game.Name, Is.EqualTo("Counter-Strike 2"));
            Assert.That(game.LogoAssetId, Is.EqualTo(asset.Id));
            Assert.That(Store.Games.Single().Id, Is.EqualTo("cs2"));
            Assert.That(UnitOfWork.SaveChangesCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    [Description("Verifies that creating a game rejects duplicate ids, blank input, and unknown logo assets.")]
    public void CreateGameAsync_RejectsInvalidInput()
    {
        // Arrange
        TournamentTestData.AddGame(Store, "lol", "League of Legends");

        // Act
        Task Duplicate() => Service.CreateGameAsync("lol", "League of Legends", null);
        Task BlankId() => Service.CreateGameAsync(" ", "League of Legends", null);
        Task BlankName() => Service.CreateGameAsync("valorant", " ", null);
        Task UnknownLogo() => Service.CreateGameAsync("valorant", "Valorant", Guid.NewGuid());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<ConflictException>(Duplicate);
            Assert.ThrowsAsync<ValidationException>(BlankId);
            Assert.ThrowsAsync<ValidationException>(BlankName);
            Assert.ThrowsAsync<NotFoundException>(UnknownLogo);
        });
    }

    [Test]
    [Description("Verifies that admins can set and clear the game logo asset.")]
    public void UpdateGameLogoAsync_UpdatesLogo()
    {
        // Arrange
        var game = TournamentTestData.AddGame(Store);
        var asset = TournamentTestData.AddMediaAsset(Store);

        // Act
        var updated = Service.UpdateGameLogoAsync(game.Id, asset.Id).GetAwaiter().GetResult();
        var cleared = Service.UpdateGameLogoAsync(game.Id, null).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(updated.LogoAssetId, Is.EqualTo(asset.Id));
            Assert.That(cleared.LogoAssetId, Is.Null);
            Assert.That(Store.Games.Single().LogoAssetId, Is.Null);
            Assert.That(UnitOfWork.SaveChangesCallCount, Is.EqualTo(2));
        });
    }

    [Test]
    [Description("Verifies that admins can delete unused games.")]
    public void DeleteGameAsync_RemovesUnusedGame()
    {
        // Arrange
        TournamentTestData.AddGame(Store);

        // Act
        Service.DeleteGameAsync(TournamentTestData.GameId).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(Store.Games, Is.Empty);
            Assert.That(UnitOfWork.SaveChangesCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    [Description("Verifies that games used by tournament data cannot be deleted.")]
    public void DeleteGameAsync_RejectsGameInUse()
    {
        // Arrange
        TournamentTestData.AddGame(Store);
        TournamentTestData.AddTeam(Store, memberships: [(TournamentTestData.OwnerId, TeamRole.Owner)]);

        // Act
        Task Act() => Service.DeleteGameAsync(TournamentTestData.GameId);

        // Assert
        Assert.ThrowsAsync<ConflictException>(Act);
    }
}
