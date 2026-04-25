using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
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

    [Test]
    [Description("Verifies that the games controller passes create-game values to the application service.")]
    public async Task CreateGame_ReturnsOkWithGame()
    {
        // Arrange
        var logoAssetId = Guid.NewGuid();
        var request = new CreateGameRequest("lol", "League of Legends", logoAssetId);
        var game = new GameDto("lol", "League of Legends", logoAssetId);
        Service
            .Setup(mock => mock.CreateGameAsync("lol", "League of Legends", logoAssetId, CancellationToken.None))
            .ReturnsAsync(game);

        // Act
        var response = await Controller.CreateGame(request, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, game);
        Service.Verify(mock => mock.CreateGameAsync("lol", "League of Legends", logoAssetId, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the games controller passes logo updates to the application service.")]
    public async Task UpdateGameLogo_ReturnsOkWithGame()
    {
        // Arrange
        var logoAssetId = Guid.NewGuid();
        var request = new UpdateGameLogoRequest(logoAssetId);
        var game = new GameDto("lol", "League of Legends", logoAssetId);
        Service
            .Setup(mock => mock.UpdateGameLogoAsync("lol", logoAssetId, CancellationToken.None))
            .ReturnsAsync(game);

        // Act
        var response = await Controller.UpdateGameLogo("lol", request, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, game);
        Service.Verify(mock => mock.UpdateGameLogoAsync("lol", logoAssetId, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the games controller passes delete-game values to the application service.")]
    public async Task DeleteGame_ReturnsNoContent()
    {
        // Arrange
        Service
            .Setup(mock => mock.DeleteGameAsync("lol", CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var response = await Controller.DeleteGame("lol", CancellationToken.None);

        // Assert
        Assert.That(response, Is.InstanceOf<NoContentResult>());
        Service.Verify(mock => mock.DeleteGameAsync("lol", CancellationToken.None), Times.Once);
    }
}
