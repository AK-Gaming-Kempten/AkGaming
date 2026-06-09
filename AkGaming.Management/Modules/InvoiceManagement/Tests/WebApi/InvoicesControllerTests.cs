using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.InvoiceManagement.Api.Controllers;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.Management.Modules.InvoiceManagement.Tests.WebApi;

[TestFixture]
public sealed class InvoicesControllerTests
{
    private Mock<IInvoiceManagementService> _service = null!;
    private InvoicesController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<IInvoiceManagementService>(MockBehavior.Strict);
        _controller = new InvoicesController(_service.Object);
    }

    [Test]
    [Description("Returns a created response containing the persisted invoice.")]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedInvoice()
    {
        // Arrange
        var request = CreateInvoice();
        var created = CreateInvoice();
        created.Id = Guid.NewGuid();
        _service.Setup(service => service.CreateInvoiceAsync(request, CancellationToken.None))
            .ReturnsAsync(Result<InvoiceDetailsDto>.Success(created));

        // Act
        var response = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = response.Result as CreatedAtActionResult;
        Assert.That(createdResult, Is.Not.Null);
        Assert.That(createdResult!.Value, Is.SameAs(created));
    }

    [Test]
    [Description("Returns a bad request when invoice validation fails in the service.")]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateInvoice();
        _service.Setup(service => service.CreateInvoiceAsync(request, CancellationToken.None))
            .ReturnsAsync(Result<InvoiceDetailsDto>.Failure("Invoice number is required."));

        // Act
        var response = await _controller.Create(request, CancellationToken.None);

        // Assert
        Assert.That(response.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    [Description("Returns a PDF file with an invoice-specific download name.")]
    public async Task RenderPdf_WhenInvoiceExists_ReturnsPdfFile()
    {
        // Arrange
        var invoice = CreateInvoice();
        invoice.Id = Guid.NewGuid();
        var pdf = new byte[] { 1, 2, 3 };
        _service.Setup(service => service.GetInvoiceAsync(invoice.Id, CancellationToken.None))
            .ReturnsAsync(Result<InvoiceDetailsDto>.Success(invoice));
        _service.Setup(service => service.RenderPdf(invoice))
            .Returns(Result<byte[]>.Success(pdf));

        // Act
        var response = await _controller.RenderPdf(invoice.Id, CancellationToken.None);

        // Assert
        var fileResult = response as FileContentResult;
        Assert.That(fileResult, Is.Not.Null);
        Assert.That(fileResult!.FileContents, Is.EqualTo(pdf));
        Assert.That(fileResult.FileDownloadName, Is.EqualTo("invoice-INV-1.pdf"));
    }

    private static InvoiceDetailsDto CreateInvoice()
    {
        return new InvoiceDetailsDto
        {
            InvoiceNumber = "INV-1",
            Seller = new InvoicePartyDto { Name = "Seller", Street = "Street 1", PostalCode = "10000", City = "Berlin" },
            Buyer = new InvoicePartyDto { Name = "Buyer", Street = "Street 2", PostalCode = "20000", City = "Hamburg" },
            LineItems = [new InvoiceLineItemDto { Description = "Service", UnitPrice = 10m, Quantity = 1m }]
        };
    }
}
