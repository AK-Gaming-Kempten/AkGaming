using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.Tournaments.Tests.WebApi;

public sealed class TournamentRegistrationsControllerTests
{
    private Mock<ITournamentRegistrationService> Service { get; set; } = null!;
    private TournamentRegistrationsController Controller { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Service = new Mock<ITournamentRegistrationService>();
        Controller = new TournamentRegistrationsController(Service.Object);
    }

    [Test]
    [Description("Verifies that the tournament registration controller returns no registration as not found.")]
    public async Task GetTournamentRegistration_ReturnsNotFoundWhenServiceReturnsNull()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        Service
            .Setup(mock => mock.GetRegistrationAsync(registrationId, CancellationToken.None))
            .ReturnsAsync((TournamentRegistrationDto?)null);

        // Act
        var response = await Controller.GetTournamentRegistration(registrationId, CancellationToken.None);

        // Assert
        Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());
        Service.Verify(mock => mock.GetRegistrationAsync(registrationId, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the tournament registration controller returns an existing registration.")]
    public async Task GetTournamentRegistration_ReturnsOkWithRegistration()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var registration = WebApiControllerTestHelpers.Registration(registrationId);
        Service
            .Setup(mock => mock.GetRegistrationAsync(registrationId, CancellationToken.None))
            .ReturnsAsync(registration);

        // Act
        var response = await Controller.GetTournamentRegistration(registrationId, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, registration);
        Service.Verify(mock => mock.GetRegistrationAsync(registrationId, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the tournament registration controller passes team id to the listing use case.")]
    public async Task GetTeamRegistrations_ReturnsOkWithRegistrations()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var registrations = new List<TournamentRegistrationDto> { WebApiControllerTestHelpers.Registration(Guid.NewGuid(), teamId: teamId) };
        Service
            .Setup(mock => mock.GetTeamRegistrationsAsync(teamId, CancellationToken.None))
            .ReturnsAsync(registrations);

        // Act
        var response = await Controller.GetTeamRegistrations(teamId, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, registrations);
        Service.Verify(mock => mock.GetTeamRegistrationsAsync(teamId, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the tournament registration controller passes registration review values to the application service.")]
    public async Task ReviewTournamentRegistration_ReturnsOkWithRegistration()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var registration = WebApiControllerTestHelpers.Registration(registrationId);
        var request = new ReviewRegistrationRequest(true, "approved");
        Service
            .Setup(mock => mock.ReviewRegistrationAsync(registrationId, true, "approved", CancellationToken.None))
            .ReturnsAsync(registration);

        // Act
        var response = await Controller.ReviewTournamentRegistration(registrationId, request, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, registration);
        Service.Verify(
            mock => mock.ReviewRegistrationAsync(registrationId, true, "approved", CancellationToken.None),
            Times.Once);
    }

    [Test]
    [Description("Verifies that the tournament registration controller passes roster review values to the application service.")]
    public async Task ReviewRosterChange_ReturnsOkWithRegistration()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var rosterId = Guid.NewGuid();
        var registration = WebApiControllerTestHelpers.Registration(registrationId);
        var request = new ReviewRegistrationRequest(false, "rejected");
        Service
            .Setup(mock => mock.ReviewRosterAsync(registrationId, rosterId, false, "rejected", CancellationToken.None))
            .ReturnsAsync(registration);

        // Act
        var response = await Controller.ReviewRosterChange(registrationId, rosterId, request, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, registration);
        Service.Verify(
            mock => mock.ReviewRosterAsync(registrationId, rosterId, false, "rejected", CancellationToken.None),
            Times.Once);
    }

    [Test]
    [Description("Verifies that the tournament registration controller passes submit-roster-change values to the application service.")]
    public async Task SubmitRosterChange_ReturnsOkWithRegistration()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var profileIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var registration = WebApiControllerTestHelpers.Registration(registrationId);
        var request = new SubmitRosterChangeRequest("captain-1", profileIds);
        Service
            .Setup(mock => mock.SubmitRosterChangeAsync(registrationId, "captain-1", profileIds, CancellationToken.None))
            .ReturnsAsync(registration);

        // Act
        var response = await Controller.SubmitRosterChange(registrationId, request, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, registration);
        Service.Verify(
            mock => mock.SubmitRosterChangeAsync(registrationId, "captain-1", profileIds, CancellationToken.None),
            Times.Once);
    }

    [Test]
    [Description("Verifies that the tournament registration controller passes initial registration values to the application service.")]
    public async Task SubmitTournamentRegistration_ReturnsOkWithRegistration()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var tournamentId = Guid.NewGuid();
        var profileIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var registration = WebApiControllerTestHelpers.Registration(Guid.NewGuid(), teamId, tournamentId);
        var request = new SubmitTournamentRegistrationRequest("captain-1", tournamentId, profileIds);
        Service
            .Setup(mock => mock.SubmitRegistrationAsync(teamId, tournamentId, "captain-1", profileIds, CancellationToken.None))
            .ReturnsAsync(registration);

        // Act
        var response = await Controller.SubmitTournamentRegistration(teamId, request, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, registration);
        Service.Verify(
            mock => mock.SubmitRegistrationAsync(teamId, tournamentId, "captain-1", profileIds, CancellationToken.None),
            Times.Once);
    }
}
