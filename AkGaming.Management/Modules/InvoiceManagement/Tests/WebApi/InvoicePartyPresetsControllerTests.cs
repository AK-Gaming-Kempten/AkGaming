using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.InvoiceManagement.Api.Controllers;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.Management.Modules.InvoiceManagement.Tests.WebApi;

[TestFixture]
public sealed class InvoicePartyPresetsControllerTests
{
    [Test]
    [Description("Returns not found when deleting an unknown party preset.")]
    public async Task Delete_WhenPresetDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var presetId = Guid.NewGuid();
        var service = new Mock<IInvoiceManagementService>(MockBehavior.Strict);
        service.Setup(candidate => candidate.DeletePartyPresetAsync(presetId, CancellationToken.None))
            .ReturnsAsync(Result.Failure("Invoice party preset not found."));
        var controller = new InvoicePartyPresetsController(service.Object);

        // Act
        var response = await controller.Delete(presetId, CancellationToken.None);

        // Assert
        Assert.That(response, Is.TypeOf<NotFoundObjectResult>());
    }

    [Test]
    [Description("Returns the created party preset when persistence succeeds.")]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedPreset()
    {
        // Arrange
        var preset = new InvoicePartyPresetDto
        {
            Id = Guid.NewGuid(),
            Label = "Club",
            Party = new InvoicePartyDto { Name = "AK Gaming", Street = "Street 1", PostalCode = "10000", City = "Berlin" }
        };
        var service = new Mock<IInvoiceManagementService>(MockBehavior.Strict);
        service.Setup(candidate => candidate.CreatePartyPresetAsync(preset, CancellationToken.None))
            .ReturnsAsync(Result<InvoicePartyPresetDto>.Success(preset));
        var controller = new InvoicePartyPresetsController(service.Object);

        // Act
        var response = await controller.Create(preset, CancellationToken.None);

        // Assert
        var createdResult = response.Result as CreatedResult;
        Assert.That(createdResult, Is.Not.Null);
        Assert.That(createdResult!.Value, Is.SameAs(preset));
    }
}
