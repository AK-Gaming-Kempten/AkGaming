using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.Tests.Fakes;

namespace AkGaming.Tournaments.Tests.Application;

public sealed class GameCatalogServiceTests
{

    private InMemoryStore Store { get; set; } = null!;

    private GameCatalogService Service { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Store = new InMemoryStore();
        Service = new GameCatalogService(new InMemoryGameRepository(Store));
    }


    [Test]
    [Description("Verifies that the game catalog returns public game DTOs ordered by display name.")]
    public void GetGamesAsync_ReturnsGamesOrderedByName()
    {
        TournamentTestData.AddGame(Store, "valorant", "Valorant");
        TournamentTestData.AddGame(Store, "lol", "League of Legends");
        TournamentTestData.AddGame(Store, "cs2", "Counter-Strike 2");

        var games = Service.GetGamesAsync().GetAwaiter().GetResult();

        Assert.That(games.Select(game => game.Name), Is.EqualTo(new[] { "Counter-Strike 2", "League of Legends", "Valorant" }));
    }
}
