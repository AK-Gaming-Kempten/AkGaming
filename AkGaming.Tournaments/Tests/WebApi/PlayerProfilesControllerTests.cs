using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.WebApi.Controllers;
using Moq;

namespace AkGaming.Tournaments.Tests.WebApi;

public sealed class PlayerProfilesControllerTests
{
    [Test]
    [Description("Verifies that the player profiles controller passes the route user id to the application service.")]
    public async Task GetUserPlayerProfiles_ReturnsOkWithProfiles()
    {
        var profiles = new List<Contracts.DTOs.PlayerProfileDto> { WebApiControllerTestHelpers.UserProfile() };
        var service = new Mock<IPlayerProfileManagementService>();
        service
            .Setup(mock => mock.GetUserProfilesAsync("user-1", CancellationToken.None))
            .ReturnsAsync(profiles);
        var controller = new PlayerProfilesController(service.Object);

        var response = await controller.GetUserPlayerProfiles("user-1", CancellationToken.None);

        WebApiControllerTestHelpers.AssertOkValue(response, profiles);
        service.Verify(mock => mock.GetUserProfilesAsync("user-1", CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the player profiles controller passes upsert route and request values to the application service.")]
    public async Task UpsertUserPlayerProfile_ReturnsOkWithProfile()
    {
        var profile = WebApiControllerTestHelpers.UserProfile(name: "Summoner Prime");
        var request = new UpsertUserPlayerProfileRequest("Summoner Prime");
        var service = new Mock<IPlayerProfileManagementService>();
        service
            .Setup(mock => mock.UpsertUserProfileAsync("user-1", "lol", "Summoner Prime", CancellationToken.None))
            .ReturnsAsync(profile);
        var controller = new PlayerProfilesController(service.Object);

        var response = await controller.UpsertUserPlayerProfile("user-1", "lol", request, CancellationToken.None);

        WebApiControllerTestHelpers.AssertOkValue(response, profile);
        service.Verify(
            mock => mock.UpsertUserProfileAsync("user-1", "lol", "Summoner Prime", CancellationToken.None),
            Times.Once);
    }
}
