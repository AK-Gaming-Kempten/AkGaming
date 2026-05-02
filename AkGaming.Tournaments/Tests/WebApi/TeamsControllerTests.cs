using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.Tournaments.Tests.WebApi;

public sealed class TeamsControllerTests
{
    private Mock<ITeamManagementService> Service { get; set; } = null!;
    private TeamsController Controller { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Service = new Mock<ITeamManagementService>();
        Controller = new TeamsController(Service.Object);
        WebApiControllerTestHelpers.SetAuthenticatedUser(Controller);
    }

    [Test]
    [Description("Verifies that the teams controller passes add-member values to the application service.")]
    public async Task AddTeamMember_ReturnsOkWithTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var team = WebApiControllerTestHelpers.Team(teamId);
        var request = new AddTeamMemberRequest("captain-1", "member-1", TeamRoleDto.Editor);
        Service
            .Setup(mock => mock.AddMemberAsync(teamId, "captain-1", "member-1", TeamRoleDto.Editor, CancellationToken.None))
            .ReturnsAsync(team);

        // Act
        var response = await Controller.AddTeamMember(teamId, request, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, team);
        Service.Verify(
            mock => mock.AddMemberAsync(teamId, "captain-1", "member-1", TeamRoleDto.Editor, CancellationToken.None),
            Times.Once);
    }

    [Test]
    [Description("Verifies that the teams controller passes create-team values to the application service.")]
    public async Task CreateTeam_ReturnsOkWithTeam()
    {
        // Arrange
        var team = WebApiControllerTestHelpers.Team(Guid.NewGuid());
        var request = new CreateTeamRequest("captain-1", "lol", "AKG Blue");
        Service
            .Setup(mock => mock.CreateTeamAsync("captain-1", "lol", "AKG Blue", CancellationToken.None))
            .ReturnsAsync(team);

        // Act
        var response = await Controller.CreateTeam(request, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, team);
        Service.Verify(mock => mock.CreateTeamAsync("captain-1", "lol", "AKG Blue", CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the teams controller passes create-guest-profile values to the application service.")]
    public async Task CreateGuestPlayerProfile_ReturnsOkWithProfile()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var profile = WebApiControllerTestHelpers.GuestProfile(teamId);
        var request = new CreateGuestPlayerProfileRequest("captain-1", "Guest Mid", 1200);
        Service
            .Setup(mock => mock.CreateGuestPlayerProfileAsync(teamId, "captain-1", "Guest Mid", 1200, null, CancellationToken.None))
            .ReturnsAsync(profile);

        // Act
        var response = await Controller.CreateGuestPlayerProfile(teamId, request, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, profile);
        Service.Verify(
            mock => mock.CreateGuestPlayerProfileAsync(teamId, "captain-1", "Guest Mid", 1200, null, CancellationToken.None),
            Times.Once);
    }

    [Test]
    [Description("Verifies that the teams controller passes available-profile route values to the application service.")]
    public async Task GetAvailableTeamProfiles_ReturnsOkWithProfiles()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var profiles = new List<PlayerProfileDto> { WebApiControllerTestHelpers.UserProfile() };
        Service
            .Setup(mock => mock.GetAvailableProfilesAsync(teamId, "lol", CancellationToken.None))
            .ReturnsAsync(profiles);

        // Act
        var response = await Controller.GetAvailableTeamProfiles(teamId, "lol", CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, profiles);
        Service.Verify(mock => mock.GetAvailableProfilesAsync(teamId, "lol", CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the teams controller returns not found when the application service returns no team.")]
    public async Task GetTeam_ReturnsNotFoundWhenServiceReturnsNull()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        Service
            .Setup(mock => mock.GetTeamAsync(teamId, CancellationToken.None))
            .ReturnsAsync((TeamDto?)null);

        // Act
        var response = await Controller.GetTeam(teamId, CancellationToken.None);

        // Assert
        Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());
        Service.Verify(mock => mock.GetTeamAsync(teamId, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the teams controller returns a team when the application service finds one.")]
    public async Task GetTeam_ReturnsOkWithTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var team = WebApiControllerTestHelpers.Team(teamId);
        Service
            .Setup(mock => mock.GetTeamAsync(teamId, CancellationToken.None))
            .ReturnsAsync(team);

        // Act
        var response = await Controller.GetTeam(teamId, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, team);
        Service.Verify(mock => mock.GetTeamAsync(teamId, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the teams controller passes the user id to the user-team listing service.")]
    public async Task GetUserTeams_ReturnsOkWithTeams()
    {
        // Arrange
        var teams = new List<TeamDto> { WebApiControllerTestHelpers.Team(Guid.NewGuid()) };
        Service
            .Setup(mock => mock.GetTeamsForUserAsync("captain-1", CancellationToken.None))
            .ReturnsAsync(teams);

        // Act
        var response = await Controller.GetUserTeams("captain-1", CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, teams);
        Service.Verify(mock => mock.GetTeamsForUserAsync("captain-1", CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the teams controller passes update-guest-profile values to the application service.")]
    public async Task UpdateGuestPlayerProfile_ReturnsOkWithProfile()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var profile = WebApiControllerTestHelpers.GuestProfile(teamId, profileId, "Guest ADC");
        var request = new UpdateGuestPlayerProfileRequest("captain-1", "Guest ADC", 1300);
        Service
            .Setup(mock => mock.UpdateGuestPlayerProfileAsync(teamId, profileId, "captain-1", "Guest ADC", 1300, null, CancellationToken.None))
            .ReturnsAsync(profile);

        // Act
        var response = await Controller.UpdateGuestPlayerProfile(teamId, profileId, request, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, profile);
        Service.Verify(
            mock => mock.UpdateGuestPlayerProfileAsync(teamId, profileId, "captain-1", "Guest ADC", 1300, null, CancellationToken.None),
            Times.Once);
    }

    [Test]
    [Description("Verifies that the teams controller passes update-team values to the application service.")]
    public async Task UpdateTeam_ReturnsOkWithTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var team = WebApiControllerTestHelpers.Team(teamId);
        var request = new UpdateTeamRequest("captain-1", "AKG Crimson", null, null, null);
        Service
            .Setup(mock => mock.UpdateTeamAsync(teamId, "captain-1", "AKG Crimson", null, null, null, CancellationToken.None))
            .ReturnsAsync(team);

        // Act
        var response = await Controller.UpdateTeam(teamId, request, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, team);
        Service.Verify(
            mock => mock.UpdateTeamAsync(teamId, "captain-1", "AKG Crimson", null, null, null, CancellationToken.None),
            Times.Once);
    }

    [Test]
    [Description("Verifies that the teams controller passes delete-guest-profile values to the application service.")]
    public async Task DeleteGuestPlayerProfile_ReturnsOkWithTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var team = WebApiControllerTestHelpers.Team(teamId);
        Service
            .Setup(mock => mock.DeleteGuestPlayerProfileAsync(teamId, profileId, "captain-1", CancellationToken.None))
            .ReturnsAsync(team);

        // Act
        var response = await Controller.DeleteGuestPlayerProfile(teamId, profileId, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, team);
        Service.Verify(
            mock => mock.DeleteGuestPlayerProfileAsync(teamId, profileId, "captain-1", CancellationToken.None),
            Times.Once);
    }

    [Test]
    [Description("Verifies that the teams controller passes update-member-role values to the application service.")]
    public async Task UpdateTeamMemberRole_ReturnsOkWithTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var team = WebApiControllerTestHelpers.Team(teamId);
        var request = new UpdateTeamMemberRoleRequest("captain-1", TeamRoleDto.Owner);
        Service
            .Setup(mock => mock.UpdateMemberRoleAsync(teamId, "captain-1", "member-1", TeamRoleDto.Owner, CancellationToken.None))
            .ReturnsAsync(team);

        // Act
        var response = await Controller.UpdateTeamMemberRole(teamId, "member-1", request, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, team);
        Service.Verify(
            mock => mock.UpdateMemberRoleAsync(teamId, "captain-1", "member-1", TeamRoleDto.Owner, CancellationToken.None),
            Times.Once);
    }
}
