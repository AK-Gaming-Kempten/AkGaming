using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.Disbursements.Api.Controllers;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.Disbursements.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.Management.Modules.Disbursements.Tests.WebApi;

[TestFixture]
public sealed class DisbursementDiscordInteractionsControllerTests
{
    private Mock<IDisbursementService> _service = null!;
    private DisbursementDiscordInteractionsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<IDisbursementService>(MockBehavior.Strict);
        _controller = new DisbursementDiscordInteractionsController(_service.Object);
    }

    [Test]
    [Description("Passes a linked Discord user's allocation decision to the disbursement service.")]
    public async Task Decide_WhenRequestIsValid_ReturnsUpdatedApplication()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var request = new DiscordAllocationDecisionRequest
        {
            UserId = Guid.NewGuid(),
            ApproverName = "Teammate",
            IsApproved = true
        };
        var application = new AllocationApplicationDto { Id = applicationId };
        _service.Setup(service => service.DecideFromDiscordAsync(applicationId, request, CancellationToken.None))
            .ReturnsAsync(Result<AllocationApplicationDto>.Success(application));

        // Act
        var response = await _controller.Decide(applicationId, request, CancellationToken.None);

        // Assert
        var ok = response.Result as OkObjectResult;
        Assert.That(ok?.Value, Is.SameAs(application));
        _service.Verify(service => service.DecideFromDiscordAsync(applicationId, request, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Returns a bad request when a Discord allocation decision is rejected by domain validation.")]
    public async Task Decide_WhenServiceRejectsDecision_ReturnsBadRequest()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var request = new DiscordAllocationDecisionRequest { UserId = Guid.NewGuid() };
        _service.Setup(service => service.DecideFromDiscordAsync(applicationId, request, CancellationToken.None))
            .ReturnsAsync(Result<AllocationApplicationDto>.Failure("Applicants cannot approve their own application."));

        // Act
        var response = await _controller.Decide(applicationId, request, CancellationToken.None);

        // Assert
        var badRequest = response.Result as BadRequestObjectResult;
        Assert.That(badRequest?.Value, Is.EqualTo("Applicants cannot approve their own application."));
    }
}
