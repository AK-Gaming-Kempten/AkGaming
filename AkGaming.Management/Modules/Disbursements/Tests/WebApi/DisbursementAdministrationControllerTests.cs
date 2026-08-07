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
}
