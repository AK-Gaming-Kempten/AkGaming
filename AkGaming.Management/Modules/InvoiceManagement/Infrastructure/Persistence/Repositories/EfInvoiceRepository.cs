using AkGaming.Management.Modules.InvoiceManagement.Application.Interfaces;
using AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence.Repositories;

public sealed class EfInvoiceRepository(InvoiceManagementDbContext dbContext) : IInvoiceRepository
{
    public async Task<IReadOnlyList<Invoice>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await InvoiceQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return invoices
            .OrderByDescending(invoice => invoice.InvoiceDate)
            .ThenByDescending(invoice => invoice.UpdatedAt)
            .ToList();
    }

    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return InvoiceQuery().SingleOrDefaultAsync(invoice => invoice.Id == id, cancellationToken);
    }

    public Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        return dbContext.Invoices.AnyAsync(
            invoice => invoice.InvoiceNumber == invoiceNumber && (!excludingId.HasValue || invoice.Id != excludingId.Value),
            cancellationToken);
    }

    public void Add(Invoice invoice) => dbContext.Invoices.Add(invoice);
    public void Remove(Invoice invoice) => dbContext.Invoices.Remove(invoice);

    public async Task<IReadOnlyList<InvoicePartyPreset>> GetPartyPresetsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.InvoicePartyPresets.AsNoTracking().OrderBy(preset => preset.Label).ToListAsync(cancellationToken);
    }

    public Task<InvoicePartyPreset?> GetPartyPresetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.InvoicePartyPresets.SingleOrDefaultAsync(preset => preset.Id == id, cancellationToken);
    }

    public Task<bool> PartyPresetLabelExistsAsync(string label, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        return dbContext.InvoicePartyPresets.AnyAsync(
            preset => preset.Label == label && (!excludingId.HasValue || preset.Id != excludingId.Value),
            cancellationToken);
    }

    public void AddPartyPreset(InvoicePartyPreset preset) => dbContext.InvoicePartyPresets.Add(preset);
    public void RemovePartyPreset(InvoicePartyPreset preset) => dbContext.InvoicePartyPresets.Remove(preset);

    public async Task<IReadOnlyList<InvoicePaymentTermsPreset>> GetPaymentTermsPresetsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.InvoicePaymentTermsPresets
            .AsNoTracking()
            .OrderBy(preset => preset.Label)
            .ToListAsync(cancellationToken);
    }

    public Task<InvoicePaymentTermsPreset?> GetPaymentTermsPresetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.InvoicePaymentTermsPresets.SingleOrDefaultAsync(preset => preset.Id == id, cancellationToken);
    }

    public Task<bool> PaymentTermsPresetLabelExistsAsync(string label, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        return dbContext.InvoicePaymentTermsPresets.AnyAsync(
            preset => preset.Label == label && (!excludingId.HasValue || preset.Id != excludingId.Value),
            cancellationToken);
    }

    public void AddPaymentTermsPreset(InvoicePaymentTermsPreset preset) => dbContext.InvoicePaymentTermsPresets.Add(preset);
    public void RemovePaymentTermsPreset(InvoicePaymentTermsPreset preset) => dbContext.InvoicePaymentTermsPresets.Remove(preset);

    public async Task<IReadOnlyList<InvoiceBankAccountPreset>> GetBankAccountPresetsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.InvoiceBankAccountPresets.AsNoTracking().OrderBy(preset => preset.Label).ToListAsync(cancellationToken);
    }

    public Task<InvoiceBankAccountPreset?> GetBankAccountPresetAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.InvoiceBankAccountPresets.SingleOrDefaultAsync(preset => preset.Id == id, cancellationToken);

    public Task<bool> BankAccountPresetLabelExistsAsync(string label, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        dbContext.InvoiceBankAccountPresets.AnyAsync(preset => preset.Label == label && (!excludingId.HasValue || preset.Id != excludingId.Value), cancellationToken);

    public void AddBankAccountPreset(InvoiceBankAccountPreset preset) => dbContext.InvoiceBankAccountPresets.Add(preset);
    public void RemoveBankAccountPreset(InvoiceBankAccountPreset preset) => dbContext.InvoiceBankAccountPresets.Remove(preset);

    public async Task<IReadOnlyList<InvoiceLineItemPreset>> GetLineItemPresetsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.InvoiceLineItemPresets.AsNoTracking().OrderBy(preset => preset.Label).ToListAsync(cancellationToken);
    }

    public Task<InvoiceLineItemPreset?> GetLineItemPresetAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.InvoiceLineItemPresets.SingleOrDefaultAsync(preset => preset.Id == id, cancellationToken);

    public Task<bool> LineItemPresetLabelExistsAsync(string label, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        dbContext.InvoiceLineItemPresets.AnyAsync(preset => preset.Label == label && (!excludingId.HasValue || preset.Id != excludingId.Value), cancellationToken);

    public void AddLineItemPreset(InvoiceLineItemPreset preset) => dbContext.InvoiceLineItemPresets.Add(preset);
    public void RemoveLineItemPreset(InvoiceLineItemPreset preset) => dbContext.InvoiceLineItemPresets.Remove(preset);

    public async Task<IReadOnlyList<InvoiceLineItemCollectionPreset>> GetLineItemCollectionPresetsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.InvoiceLineItemCollectionPresets
            .AsNoTracking()
            .Include(preset => preset.LineItems)
            .OrderBy(preset => preset.Label)
            .ToListAsync(cancellationToken);
    }

    public Task<InvoiceLineItemCollectionPreset?> GetLineItemCollectionPresetAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.InvoiceLineItemCollectionPresets
            .Include(preset => preset.LineItems)
            .SingleOrDefaultAsync(preset => preset.Id == id, cancellationToken);

    public Task<bool> LineItemCollectionPresetLabelExistsAsync(string label, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        dbContext.InvoiceLineItemCollectionPresets.AnyAsync(preset => preset.Label == label && (!excludingId.HasValue || preset.Id != excludingId.Value), cancellationToken);

    public void AddLineItemCollectionPreset(InvoiceLineItemCollectionPreset preset) => dbContext.InvoiceLineItemCollectionPresets.Add(preset);
    public void RemoveLineItemCollectionPreset(InvoiceLineItemCollectionPreset preset) => dbContext.InvoiceLineItemCollectionPresets.Remove(preset);
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Invoice> InvoiceQuery()
    {
        return dbContext.Invoices
            .Include(invoice => invoice.Parties)
            .Include(invoice => invoice.LineItems)
            .Include(invoice => invoice.BankDetails);
    }
}
