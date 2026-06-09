using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.InvoiceManagement.Api.Controllers;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.Management.Modules.InvoiceManagement.Tests.WebApi;

[TestFixture]
public sealed class InvoiceLineItemCollectionPresetsControllerTests
{
    [Test]
    [Description("Returns the created line item collection when persistence succeeds.")]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedCollection()
    {
        // Arrange
        var preset = new InvoiceLineItemCollectionPresetDto
        {
            Id = Guid.NewGuid(),
            Label = "Package",
            LineItems = [new() { Description = "Service", UnitPrice = 100m, Quantity = 1m }]
        };
        var service = new Mock<IInvoiceManagementService>(MockBehavior.Strict);
        service.Setup(candidate => candidate.CreateLineItemCollectionPresetAsync(preset, CancellationToken.None))
            .ReturnsAsync(Result<InvoiceLineItemCollectionPresetDto>.Success(preset));
        var controller = new InvoiceLineItemCollectionPresetsController(service.Object);

        // Act
        var response = await controller.Create(preset, CancellationToken.None);

        // Assert
        var createdResult = response.Result as CreatedResult;
        Assert.That(createdResult, Is.Not.Null);
        Assert.That(createdResult!.Value, Is.SameAs(preset));
    }
}
