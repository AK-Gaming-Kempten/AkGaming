using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.InvoiceManagement.Api.Controllers;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.Management.Modules.InvoiceManagement.Tests.WebApi;

[TestFixture]
public sealed class InvoicePaymentTermsPresetsControllerTests
{
    [Test]
    [Description("Returns the created payment terms preset when persistence succeeds.")]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedPreset()
    {
        // Arrange
        var preset = new InvoicePaymentTermsPresetDto
        {
            Id = Guid.NewGuid(),
            Label = "14 days",
            Terms = "Please pay within 14 days."
        };
        var service = new Mock<IInvoiceManagementService>(MockBehavior.Strict);
        service.Setup(candidate => candidate.CreatePaymentTermsPresetAsync(preset, CancellationToken.None))
            .ReturnsAsync(Result<InvoicePaymentTermsPresetDto>.Success(preset));
        var controller = new InvoicePaymentTermsPresetsController(service.Object);

        // Act
        var response = await controller.Create(preset, CancellationToken.None);

        // Assert
        var createdResult = response.Result as CreatedResult;
        Assert.That(createdResult, Is.Not.Null);
        Assert.That(createdResult!.Value, Is.SameAs(preset));
    }

    [Test]
    [Description("Returns not found when deleting an unknown payment terms preset.")]
    public async Task Delete_WhenPresetDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var presetId = Guid.NewGuid();
        var service = new Mock<IInvoiceManagementService>(MockBehavior.Strict);
        service.Setup(candidate => candidate.DeletePaymentTermsPresetAsync(presetId, CancellationToken.None))
            .ReturnsAsync(Result.Failure("Payment terms preset not found."));
        var controller = new InvoicePaymentTermsPresetsController(service.Object);

        // Act
        var response = await controller.Delete(presetId, CancellationToken.None);

        // Assert
        Assert.That(response, Is.TypeOf<NotFoundObjectResult>());
    }
}
