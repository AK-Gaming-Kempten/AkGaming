using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.WebApi.Controllers;
using Moq;

namespace AkGaming.Tournaments.Tests.WebApi;

public sealed class GamesControllerTests
{
    [Test]
    [Description("Verifies that the games controller returns the game catalog from the application service.")]
    public async Task GetGames_ReturnsOkWithGames()
    {
        var games = new List<GameDto> { new("lol", "League of Legends", null) };
        var service = new Mock<IGameCatalogService>();
        service
            .Setup(mock => mock.GetGamesAsync(CancellationToken.None))
            .ReturnsAsync(games);
        var controller = new GamesController(service.Object);

        var response = await controller.GetGames(CancellationToken.None);

        WebApiControllerTestHelpers.AssertOkValue(response, games);
        service.Verify(mock => mock.GetGamesAsync(CancellationToken.None), Times.Once);
    }
}
