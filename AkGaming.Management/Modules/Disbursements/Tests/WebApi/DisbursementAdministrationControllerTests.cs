using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.Disbursements.Api.Controllers;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.Disbursements.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.Management.Modules.Disbursements.Tests.WebApi;

[TestFixture]
public sealed class DisbursementAdministrationControllerTests
{
    [Test]
    [Description("Passes an allocation edit request to the disbursement service and returns the updated allocation.")]
    public async Task UpdateAllocation_WhenRequestIsValid_ReturnsUpdatedAllocation()
    {
        // Arrange
        var allocationId = Guid.NewGuid();
        var request = new SaveAllocationRequest { Name = "Updated prize", Amount = 250m };
        var allocation = new AllocationDto { Id = allocationId, Name = request.Name, Amount = request.Amount };
        var service = new Mock<IDisbursementService>(MockBehavior.Strict);
        service.Setup(item => item.UpdateAllocationAsync(allocationId, request, CancellationToken.None))
            .ReturnsAsync(Result<AllocationDto>.Success(allocation));
        var controller = new DisbursementAdministrationController(service.Object);

        // Act
        var response = await controller.UpdateAllocation(allocationId, request, CancellationToken.None);

        // Assert
        var ok = response.Result as OkObjectResult;
        Assert.That(ok?.Value, Is.SameAs(allocation));
        service.Verify(item => item.UpdateAllocationAsync(allocationId, request, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Queues the requested allocation announcement and returns no content.")]
    public async Task SendAllocationNotification_WhenConfigured_ReturnsNoContent()
    {
        // Arrange
        var allocationId = Guid.NewGuid();
        var service = new Mock<IDisbursementService>(MockBehavior.Strict);
        service.Setup(item => item.SendAllocationNotificationAsync(allocationId, CancellationToken.None))
            .ReturnsAsync(Result.Success());
        var controller = new DisbursementAdministrationController(service.Object);

        // Act
        var response = await controller.SendAllocationNotification(allocationId, CancellationToken.None);

        // Assert
        Assert.That(response, Is.TypeOf<NoContentResult>());
        service.Verify(item => item.SendAllocationNotificationAsync(allocationId, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Passes an administrator's claim adjustment to the disbursement service.")]
    public async Task UpdateApplication_WhenRequestIsValid_ReturnsUpdatedClaim()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var request = new UpdateAllocationApplicationRequest { Amount = 175m, Note = "Adjusted" };
        var application = new AllocationApplicationDto { Id = applicationId, Amount = request.Amount };
        var service = new Mock<IDisbursementService>(MockBehavior.Strict);
        service.Setup(item => item.UpdateAllocationApplicationAsync(
                applicationId, request, CancellationToken.None))
            .ReturnsAsync(Result<AllocationApplicationDto>.Success(application));
        var controller = new DisbursementAdministrationController(service.Object);

        // Act
        var response = await controller.UpdateApplication(
            applicationId, request, CancellationToken.None);

        // Assert
        var ok = response.Result as OkObjectResult;
        Assert.That(ok?.Value, Is.SameAs(application));
        service.Verify(item => item.UpdateAllocationApplicationAsync(
            applicationId, request, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Passes an administrator's claim cancellation to the disbursement service.")]
    public async Task CancelApplication_WhenClaimIsActive_ReturnsCancelledClaim()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var application = new AllocationApplicationDto { Id = applicationId };
        var service = new Mock<IDisbursementService>(MockBehavior.Strict);
        service.Setup(item => item.CancelAllocationApplicationAsync(applicationId, CancellationToken.None))
            .ReturnsAsync(Result<AllocationApplicationDto>.Success(application));
        var controller = new DisbursementAdministrationController(service.Object);

        // Act
        var response = await controller.CancelApplication(applicationId, CancellationToken.None);

        // Assert
        var ok = response.Result as OkObjectResult;
        Assert.That(ok?.Value, Is.SameAs(application));
        service.Verify(item => item.CancelAllocationApplicationAsync(
            applicationId, CancellationToken.None), Times.Once);
    }
}
