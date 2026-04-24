using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.Tournaments.Tests.WebApi;

public sealed class TeamsControllerTests
{
    [Test]
    [Description("Verifies that the teams controller passes add-member values to the application service.")]
    public async Task AddTeamMember_ReturnsOkWithTeam()
    {
        var teamId = Guid.NewGuid();
        var team = WebApiControllerTestHelpers.Team(teamId);
        var request = new AddTeamMemberRequest("captain-1", "member-1", TeamRoleDto.Editor);
        var service = new Mock<ITeamManagementService>();
        service
            .Setup(mock => mock.AddMemberAsync(teamId, "captain-1", "member-1", TeamRoleDto.Editor, CancellationToken.None))
            .ReturnsAsync(team);
        var controller = new TeamsController(service.Object);

        var response = await controller.AddTeamMember(teamId, request, CancellationToken.None);

        WebApiControllerTestHelpers.AssertOkValue(response, team);
        service.Verify(
            mock => mock.AddMemberAsync(teamId, "captain-1", "member-1", TeamRoleDto.Editor, CancellationToken.None),
            Times.Once);
    }

    [Test]
    [Description("Verifies that the teams controller passes create-team values to the application service.")]
    public async Task CreateTeam_ReturnsOkWithTeam()
    {
        var team = WebApiControllerTestHelpers.Team(Guid.NewGuid());
        var request = new CreateTeamRequest("captain-1", "lol", "AKG Blue");
        var service = new Mock<ITeamManagementService>();
        service
            .Setup(mock => mock.CreateTeamAsync("captain-1", "lol", "AKG Blue", CancellationToken.None))
            .ReturnsAsync(team);
        var controller = new TeamsController(service.Object);

        var response = await controller.CreateTeam(request, CancellationToken.None);

        WebApiControllerTestHelpers.AssertOkValue(response, team);
        service.Verify(mock => mock.CreateTeamAsync("captain-1", "lol", "AKG Blue", CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the teams controller passes create-guest-profile values to the application service.")]
    public async Task CreateGuestPlayerProfile_ReturnsOkWithProfile()
    {
        var teamId = Guid.NewGuid();
        var profile = WebApiControllerTestHelpers.GuestProfile(teamId);
        var request = new CreateGuestPlayerProfileRequest("captain-1", "Guest Mid");
        var service = new Mock<ITeamManagementService>();
        service
            .Setup(mock => mock.CreateGuestPlayerProfileAsync(teamId, "captain-1", "Guest Mid", CancellationToken.None))
            .ReturnsAsync(profile);
        var controller = new TeamsController(service.Object);

        var response = await controller.CreateGuestPlayerProfile(teamId, request, CancellationToken.None);

        WebApiControllerTestHelpers.AssertOkValue(response, profile);
        service.Verify(
            mock => mock.CreateGuestPlayerProfileAsync(teamId, "captain-1", "Guest Mid", CancellationToken.None),
            Times.Once);
    }

    [Test]
    [Description("Verifies that the teams controller passes available-profile route values to the application service.")]
    public async Task GetAvailableTeamProfiles_ReturnsOkWithProfiles()
    {
        var teamId = Guid.NewGuid();
        var profiles = new List<PlayerProfileDto> { WebApiControllerTestHelpers.UserProfile() };
        var service = new Mock<ITeamManagementService>();
        service
            .Setup(mock => mock.GetAvailableProfilesAsync(teamId, "lol", CancellationToken.None))
            .ReturnsAsync(profiles);
        var controller = new TeamsController(service.Object);

        var response = await controller.GetAvailableTeamProfiles(teamId, "lol", CancellationToken.None);

        WebApiControllerTestHelpers.AssertOkValue(response, profiles);
        service.Verify(mock => mock.GetAvailableProfilesAsync(teamId, "lol", CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the teams controller returns not found when the application service returns no team.")]
    public async Task GetTeam_ReturnsNotFoundWhenServiceReturnsNull()
    {
        var teamId = Guid.NewGuid();
        var service = new Mock<ITeamManagementService>();
        service
            .Setup(mock => mock.GetTeamAsync(teamId, CancellationToken.None))
            .ReturnsAsync((TeamDto?)null);
        var controller = new TeamsController(service.Object);

        var response = await controller.GetTeam(teamId, CancellationToken.None);

        Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());
        service.Verify(mock => mock.GetTeamAsync(teamId, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the teams controller returns a team when the application service finds one.")]
    public async Task GetTeam_ReturnsOkWithTeam()
    {
        var teamId = Guid.NewGuid();
        var team = WebApiControllerTestHelpers.Team(teamId);
        var service = new Mock<ITeamManagementService>();
        service
            .Setup(mock => mock.GetTeamAsync(teamId, CancellationToken.None))
            .ReturnsAsync(team);
        var controller = new TeamsController(service.Object);

        var response = await controller.GetTeam(teamId, CancellationToken.None);

        WebApiControllerTestHelpers.AssertOkValue(response, team);
        service.Verify(mock => mock.GetTeamAsync(teamId, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the teams controller passes update-guest-profile values to the application service.")]
    public async Task UpdateGuestPlayerProfile_ReturnsOkWithProfile()
    {
        var teamId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var profile = WebApiControllerTestHelpers.GuestProfile(teamId, profileId, "Guest ADC");
        var request = new UpdateGuestPlayerProfileRequest("captain-1", "Guest ADC");
        var service = new Mock<ITeamManagementService>();
        service
            .Setup(mock => mock.UpdateGuestPlayerProfileAsync(teamId, profileId, "captain-1", "Guest ADC", CancellationToken.None))
            .ReturnsAsync(profile);
        var controller = new TeamsController(service.Object);

        var response = await controller.UpdateGuestPlayerProfile(teamId, profileId, request, CancellationToken.None);

        WebApiControllerTestHelpers.AssertOkValue(response, profile);
        service.Verify(
            mock => mock.UpdateGuestPlayerProfileAsync(teamId, profileId, "captain-1", "Guest ADC", CancellationToken.None),
            Times.Once);
    }

    [Test]
    [Description("Verifies that the teams controller passes update-member-role values to the application service.")]
    public async Task UpdateTeamMemberRole_ReturnsOkWithTeam()
    {
        var teamId = Guid.NewGuid();
        var team = WebApiControllerTestHelpers.Team(teamId);
        var request = new UpdateTeamMemberRoleRequest("captain-1", TeamRoleDto.Owner);
        var service = new Mock<ITeamManagementService>();
        service
            .Setup(mock => mock.UpdateMemberRoleAsync(teamId, "captain-1", "member-1", TeamRoleDto.Owner, CancellationToken.None))
            .ReturnsAsync(team);
        var controller = new TeamsController(service.Object);

        var response = await controller.UpdateTeamMemberRole(teamId, "member-1", request, CancellationToken.None);

        WebApiControllerTestHelpers.AssertOkValue(response, team);
        service.Verify(
            mock => mock.UpdateMemberRoleAsync(teamId, "captain-1", "member-1", TeamRoleDto.Owner, CancellationToken.None),
            Times.Once);
    }
}
