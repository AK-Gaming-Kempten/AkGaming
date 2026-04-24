using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.WebApi.Controllers;
using Moq;

namespace AkGaming.Tournaments.Tests.WebApi;

public sealed class GamesControllerTests
{
    private Mock<IGameCatalogService> Service { get; set; } = null!;
    private GamesController Controller { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Service = new Mock<IGameCatalogService>();
        Controller = new GamesController(Service.Object);
    }

    [Test]
    [Description("Verifies that the games controller returns the game catalog from the application service.")]
    public async Task GetGames_ReturnsOkWithGames()
    {
        // Arrange
        var games = new List<GameDto> { new("lol", "League of Legends", null) };
        Service
            .Setup(mock => mock.GetGamesAsync(CancellationToken.None))
            .ReturnsAsync(games);

        // Act
        var response = await Controller.GetGames(CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, games);
        Service.Verify(mock => mock.GetGamesAsync(CancellationToken.None), Times.Once);
    }
}
