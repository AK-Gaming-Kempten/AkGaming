using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.Tournaments.Tests.WebApi;

public sealed class TournamentRegistrationsControllerTests
{
    [Test]
    [Description("Verifies that the tournament registration controller returns no registration as not found.")]
    public async Task GetTournamentRegistration_ReturnsNotFoundWhenServiceReturnsNull()
    {
        var registrationId = Guid.NewGuid();
        var service = new Mock<ITournamentRegistrationService>();
        service
            .Setup(mock => mock.GetRegistrationAsync(registrationId, CancellationToken.None))
            .ReturnsAsync((TournamentRegistrationDto?)null);
        var controller = new TournamentRegistrationsController(service.Object);

        var response = await controller.GetTournamentRegistration(registrationId, CancellationToken.None);

        Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());
        service.Verify(mock => mock.GetRegistrationAsync(registrationId, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the tournament registration controller returns an existing registration.")]
    public async Task GetTournamentRegistration_ReturnsOkWithRegistration()
    {
        var registrationId = Guid.NewGuid();
        var registration = WebApiControllerTestHelpers.Registration(registrationId);
        var service = new Mock<ITournamentRegistrationService>();
        service
            .Setup(mock => mock.GetRegistrationAsync(registrationId, CancellationToken.None))
            .ReturnsAsync(registration);
        var controller = new TournamentRegistrationsController(service.Object);

        var response = await controller.GetTournamentRegistration(registrationId, CancellationToken.None);

        WebApiControllerTestHelpers.AssertOkValue(response, registration);
        service.Verify(mock => mock.GetRegistrationAsync(registrationId, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the tournament registration controller passes team id to the listing use case.")]
    public async Task GetTeamRegistrations_ReturnsOkWithRegistrations()
    {
        var teamId = Guid.NewGuid();
        var registrations = new List<TournamentRegistrationDto> { WebApiControllerTestHelpers.Registration(Guid.NewGuid(), teamId: teamId) };
        var service = new Mock<ITournamentRegistrationService>();
        service
            .Setup(mock => mock.GetTeamRegistrationsAsync(teamId, CancellationToken.None))
            .ReturnsAsync(registrations);
        var controller = new TournamentRegistrationsController(service.Object);

        var response = await controller.GetTeamRegistrations(teamId, CancellationToken.None);

        WebApiControllerTestHelpers.AssertOkValue(response, registrations);
        service.Verify(mock => mock.GetTeamRegistrationsAsync(teamId, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the tournament registration controller passes registration review values to the application service.")]
    public async Task ReviewTournamentRegistration_ReturnsOkWithRegistration()
    {
        var registrationId = Guid.NewGuid();
        var registration = WebApiControllerTestHelpers.Registration(registrationId);
        var request = new ReviewRegistrationRequest(true, "approved");
        var service = new Mock<ITournamentRegistrationService>();
        service
            .Setup(mock => mock.ReviewRegistrationAsync(registrationId, true, "approved", CancellationToken.None))
            .ReturnsAsync(registration);
        var controller = new TournamentRegistrationsController(service.Object);

        var response = await controller.ReviewTournamentRegistration(registrationId, request, CancellationToken.None);

        WebApiControllerTestHelpers.AssertOkValue(response, registration);
        service.Verify(
            mock => mock.ReviewRegistrationAsync(registrationId, true, "approved", CancellationToken.None),
            Times.Once);
    }

    [Test]
    [Description("Verifies that the tournament registration controller passes roster review values to the application service.")]
    public async Task ReviewRosterChange_ReturnsOkWithRegistration()
    {
        var registrationId = Guid.NewGuid();
        var rosterId = Guid.NewGuid();
        var registration = WebApiControllerTestHelpers.Registration(registrationId);
        var request = new ReviewRegistrationRequest(false, "rejected");
        var service = new Mock<ITournamentRegistrationService>();
        service
            .Setup(mock => mock.ReviewRosterAsync(registrationId, rosterId, false, "rejected", CancellationToken.None))
            .ReturnsAsync(registration);
        var controller = new TournamentRegistrationsController(service.Object);

        var response = await controller.ReviewRosterChange(registrationId, rosterId, request, CancellationToken.None);

        WebApiControllerTestHelpers.AssertOkValue(response, registration);
        service.Verify(
            mock => mock.ReviewRosterAsync(registrationId, rosterId, false, "rejected", CancellationToken.None),
            Times.Once);
    }

    [Test]
    [Description("Verifies that the tournament registration controller passes submit-roster-change values to the application service.")]
    public async Task SubmitRosterChange_ReturnsOkWithRegistration()
    {
        var registrationId = Guid.NewGuid();
        var profileIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var registration = WebApiControllerTestHelpers.Registration(registrationId);
        var request = new SubmitRosterChangeRequest("captain-1", profileIds);
        var service = new Mock<ITournamentRegistrationService>();
        service
            .Setup(mock => mock.SubmitRosterChangeAsync(registrationId, "captain-1", profileIds, CancellationToken.None))
            .ReturnsAsync(registration);
        var controller = new TournamentRegistrationsController(service.Object);

        var response = await controller.SubmitRosterChange(registrationId, request, CancellationToken.None);

        WebApiControllerTestHelpers.AssertOkValue(response, registration);
        service.Verify(
            mock => mock.SubmitRosterChangeAsync(registrationId, "captain-1", profileIds, CancellationToken.None),
            Times.Once);
    }

    [Test]
    [Description("Verifies that the tournament registration controller passes initial registration values to the application service.")]
    public async Task SubmitTournamentRegistration_ReturnsOkWithRegistration()
    {
        var teamId = Guid.NewGuid();
        var tournamentId = Guid.NewGuid();
        var profileIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var registration = WebApiControllerTestHelpers.Registration(Guid.NewGuid(), teamId, tournamentId);
        var request = new SubmitTournamentRegistrationRequest("captain-1", tournamentId, profileIds);
        var service = new Mock<ITournamentRegistrationService>();
        service
            .Setup(mock => mock.SubmitRegistrationAsync(teamId, tournamentId, "captain-1", profileIds, CancellationToken.None))
            .ReturnsAsync(registration);
        var controller = new TournamentRegistrationsController(service.Object);

        var response = await controller.SubmitTournamentRegistration(teamId, request, CancellationToken.None);

        WebApiControllerTestHelpers.AssertOkValue(response, registration);
        service.Verify(
            mock => mock.SubmitRegistrationAsync(teamId, tournamentId, "captain-1", profileIds, CancellationToken.None),
            Times.Once);
    }
}
