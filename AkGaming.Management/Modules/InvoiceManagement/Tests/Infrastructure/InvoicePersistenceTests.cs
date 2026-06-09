using AkGaming.InvoiceGenerator.Core.Rendering;
using AkGaming.Management.Modules.InvoiceManagement.Application.Services;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence;
using AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AkGaming.Management.Modules.InvoiceManagement.Tests.Infrastructure;

[TestFixture]
public sealed class InvoicePersistenceTests
{
    [Test]
    [Description("Bank account and line item presets can be created and loaded through SQLite persistence.")]
    public async Task BankAndLineItemPresets_CreateAndList_PersistValues()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InvoiceManagementDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new InvoiceManagementDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        var service = CreateService(dbContext);
        var bankRequest = new InvoiceBankAccountPresetDto { Label = "Club account", BankDetails = new() { Iban = "DE001234", AccountHolder = "AK Gaming e.V." } };
        var itemRequest = new InvoiceLineItemPresetDto { Label = "Sponsoring", LineItem = new() { Description = "Sponsoring package", UnitPrice = 250m, Quantity = 1m } };

        // Act
        var bankCreate = await service.CreateBankAccountPresetAsync(bankRequest);
        var itemCreate = await service.CreateLineItemPresetAsync(itemRequest);
        var banks = await service.GetBankAccountPresetsAsync();
        var items = await service.GetLineItemPresetsAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(bankCreate.IsSuccess, Is.True);
            Assert.That(itemCreate.IsSuccess, Is.True);
            Assert.That(banks.Value![0].BankDetails.Iban, Is.EqualTo("DE001234"));
            Assert.That(items.Value![0].LineItem.UnitPrice, Is.EqualTo(250m));
        });
    }

    [Test]
    [Description("Editing a line item collection replaces its ordered child rows through SQLite persistence.")]
    public async Task LineItemCollectionPreset_Update_ReplacesOrderedItems()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InvoiceManagementDbContext>().UseSqlite(connection).Options;
        InvoiceLineItemCollectionPresetDto created;
        await using (var createContext = new InvoiceManagementDbContext(options))
        {
            await createContext.Database.EnsureCreatedAsync();
            var createService = CreateService(createContext);
            var result = await createService.CreateLineItemCollectionPresetAsync(new InvoiceLineItemCollectionPresetDto
            {
                Label = "Event package",
                LineItems = [new() { Description = "Old item", UnitPrice = 10m, Quantity = 1m }]
            });
            created = result.Value!;
        }

        created.LineItems =
        [
            new() { Description = "First", UnitPrice = 20m, Quantity = 2m },
            new() { Description = "Second", UnitPrice = 5m, Quantity = 3m }
        ];

        // Act
        await using (var updateContext = new InvoiceManagementDbContext(options))
        {
            var updateService = CreateService(updateContext);
            await updateService.UpdateLineItemCollectionPresetAsync(created.Id, created);
        }
        await using var readContext = new InvoiceManagementDbContext(options);
        var readService = CreateService(readContext);
        var resultAfterUpdate = await readService.GetLineItemCollectionPresetsAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(resultAfterUpdate.Value, Has.Count.EqualTo(1));
            Assert.That(resultAfterUpdate.Value![0].LineItems, Has.Count.EqualTo(2));
            Assert.That(resultAfterUpdate.Value[0].LineItems[0].Description, Is.EqualTo("First"));
            Assert.That(resultAfterUpdate.Value[0].LineItems[1].Description, Is.EqualTo("Second"));
        });
    }

    [Test]
    [Description("Payment terms presets can be created and loaded through SQLite persistence.")]
    public async Task PaymentTermsPreset_CreateAndList_PersistsPreset()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InvoiceManagementDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var dbContext = new InvoiceManagementDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        var service = CreateService(dbContext);
        var request = new InvoicePaymentTermsPresetDto
        {
            Label = "Immediate",
            Terms = "Payment is due immediately without deduction."
        };

        // Act
        var createResult = await service.CreatePaymentTermsPresetAsync(request);
        var listResult = await service.GetPaymentTermsPresetsAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(createResult.IsSuccess, Is.True);
            Assert.That(listResult.Value, Has.Count.EqualTo(1));
            Assert.That(listResult.Value![0].Label, Is.EqualTo("Immediate"));
            Assert.That(listResult.Value[0].Terms, Is.EqualTo(request.Terms));
        });
    }

    [Test]
    [Description("Payment terms preset labels must be unique before changes reach the database.")]
    public async Task PaymentTermsPreset_WithDuplicateLabel_ReturnsFailure()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InvoiceManagementDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var dbContext = new InvoiceManagementDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        var service = CreateService(dbContext);
        var first = new InvoicePaymentTermsPresetDto { Label = "30 days", Terms = "Pay within 30 days." };
        var duplicate = new InvoicePaymentTermsPresetDto { Label = "30 days", Terms = "Different text." };
        await service.CreatePaymentTermsPresetAsync(first);

        // Act
        var result = await service.CreatePaymentTermsPresetAsync(duplicate);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Does.Contain("already exists"));
        });
    }

    [Test]
    [Description("Listing invoices orders DateTimeOffset values on the client so SQLite can execute the query.")]
    public async Task GetInvoices_WithSqlite_ReturnsInvoicesWithoutTranslationFailure()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InvoiceManagementDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var dbContext = new InvoiceManagementDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        var service = CreateService(dbContext);
        await service.CreateInvoiceAsync(CreateInvoice());

        // Act
        var result = await service.GetInvoicesAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Has.Count.EqualTo(1));
            Assert.That(result.Value![0].InvoiceNumber, Is.EqualTo("INV-PERSISTENCE-1"));
        });
    }

    [Test]
    [Description("Editing an invoice replaces its party snapshots and line items without violating database constraints.")]
    public async Task UpdateInvoice_ReplacesOwnedRows_AndPersistsEditedDocument()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InvoiceManagementDbContext>()
            .UseSqlite(connection)
            .Options;
        var createRequest = CreateInvoice();
        InvoiceDetailsDto createdInvoice;
        await using (var createContext = new InvoiceManagementDbContext(options))
        {
            await createContext.Database.EnsureCreatedAsync();
            var createService = CreateService(createContext);
            var createResult = await createService.CreateInvoiceAsync(createRequest);
            createdInvoice = createResult.Value!;
        }

        var invoiceId = createdInvoice.Id;
        var updateRequest = createdInvoice;
        updateRequest.Buyer.Name = "Edited buyer";
        updateRequest.LineItems =
        [
            new InvoiceLineItemDto { Description = "First", UnitPrice = 25m, Quantity = 2m },
            new InvoiceLineItemDto { Description = "Second", UnitPrice = 5m, Quantity = 1m }
        ];

        // Act
        AkGaming.Core.Common.Generics.Result<InvoiceDetailsDto> updateResult;
        await using (var updateContext = new InvoiceManagementDbContext(options))
        {
            var updateService = CreateService(updateContext);
            updateResult = await updateService.UpdateInvoiceAsync(invoiceId, updateRequest);
        }

        await using var readContext = new InvoiceManagementDbContext(options);
        var readService = CreateService(readContext);
        var persistedResult = await readService.GetInvoiceAsync(invoiceId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(updateResult.IsSuccess, Is.True);
            Assert.That(persistedResult.Value!.Buyer.Name, Is.EqualTo("Edited buyer"));
            Assert.That(persistedResult.Value.LineItems, Has.Count.EqualTo(2));
            Assert.That(persistedResult.Value.LineItems.Sum(item => item.TotalPrice), Is.EqualTo(55m));
        });
    }

    private static InvoiceManagementService CreateService(InvoiceManagementDbContext dbContext)
    {
        return new InvoiceManagementService(
            new EfInvoiceRepository(dbContext),
            Mock.Of<IInvoiceHtmlRenderer>(),
            Mock.Of<IInvoicePdfRenderer>());
    }

    private static InvoiceDetailsDto CreateInvoice()
    {
        return new InvoiceDetailsDto
        {
            InvoiceNumber = "INV-PERSISTENCE-1",
            Seller = new InvoicePartyDto { Name = "Seller", Street = "Street 1", PostalCode = "10000", City = "Berlin" },
            Buyer = new InvoicePartyDto { Name = "Buyer", Street = "Street 2", PostalCode = "20000", City = "Hamburg" },
            LineItems = [new InvoiceLineItemDto { Description = "Service", UnitPrice = 10m, Quantity = 1m }]
        };
    }
}
