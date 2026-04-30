using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.WebApi.Controllers;
using Moq;

namespace AkGaming.Tournaments.Tests.WebApi;

public sealed class PlayerProfilesControllerTests
{
    private Mock<IPlayerProfileManagementService> Service { get; set; } = null!;
    private PlayerProfilesController Controller { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Service = new Mock<IPlayerProfileManagementService>();
        Controller = new PlayerProfilesController(Service.Object);
    }

    [Test]
    [Description("Verifies that the player profiles controller passes the route user id to the application service.")]
    public async Task GetUserPlayerProfiles_ReturnsOkWithProfiles()
    {
        // Arrange
        var profiles = new List<Contracts.DTOs.PlayerProfileDto> { WebApiControllerTestHelpers.UserProfile() };
        Service
            .Setup(mock => mock.GetUserProfilesAsync("user-1", CancellationToken.None))
            .ReturnsAsync(profiles);

        // Act
        var response = await Controller.GetUserPlayerProfiles("user-1", CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, profiles);
        Service.Verify(mock => mock.GetUserProfilesAsync("user-1", CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the player profiles controller passes upsert route and request values to the application service.")]
    public async Task UpsertUserPlayerProfile_ReturnsOkWithProfile()
    {
        // Arrange
        var profile = WebApiControllerTestHelpers.UserProfile(name: "Summoner Prime");
        var request = new UpsertUserPlayerProfileRequest("Summoner Prime", 1599);
        Service
            .Setup(mock => mock.UpsertUserProfileAsync("user-1", "lol", "Summoner Prime", 1599, null, CancellationToken.None))
            .ReturnsAsync(profile);

        // Act
        var response = await Controller.UpsertUserPlayerProfile("user-1", "lol", request, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, profile);
        Service.Verify(
            mock => mock.UpsertUserProfileAsync("user-1", "lol", "Summoner Prime", 1599, null, CancellationToken.None),
            Times.Once);
    }
}
