using AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;

namespace AkGaming.Management.Modules.InvoiceManagement.Application.Interfaces;

public interface IInvoiceRepository
{
    Task<IReadOnlyList<Invoice>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, Guid? excludingId = null, CancellationToken cancellationToken = default);
    void Add(Invoice invoice);
    void Remove(Invoice invoice);
    Task<IReadOnlyList<InvoicePartyPreset>> GetPartyPresetsAsync(CancellationToken cancellationToken = default);
    Task<InvoicePartyPreset?> GetPartyPresetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> PartyPresetLabelExistsAsync(string label, Guid? excludingId = null, CancellationToken cancellationToken = default);
    void AddPartyPreset(InvoicePartyPreset preset);
    void RemovePartyPreset(InvoicePartyPreset preset);
    Task<IReadOnlyList<InvoicePaymentTermsPreset>> GetPaymentTermsPresetsAsync(CancellationToken cancellationToken = default);
    Task<InvoicePaymentTermsPreset?> GetPaymentTermsPresetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> PaymentTermsPresetLabelExistsAsync(string label, Guid? excludingId = null, CancellationToken cancellationToken = default);
    void AddPaymentTermsPreset(InvoicePaymentTermsPreset preset);
    void RemovePaymentTermsPreset(InvoicePaymentTermsPreset preset);
    Task<IReadOnlyList<InvoiceBankAccountPreset>> GetBankAccountPresetsAsync(CancellationToken cancellationToken = default);
    Task<InvoiceBankAccountPreset?> GetBankAccountPresetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> BankAccountPresetLabelExistsAsync(string label, Guid? excludingId = null, CancellationToken cancellationToken = default);
    void AddBankAccountPreset(InvoiceBankAccountPreset preset);
    void RemoveBankAccountPreset(InvoiceBankAccountPreset preset);
    Task<IReadOnlyList<InvoiceLineItemPreset>> GetLineItemPresetsAsync(CancellationToken cancellationToken = default);
    Task<InvoiceLineItemPreset?> GetLineItemPresetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> LineItemPresetLabelExistsAsync(string label, Guid? excludingId = null, CancellationToken cancellationToken = default);
    void AddLineItemPreset(InvoiceLineItemPreset preset);
    void RemoveLineItemPreset(InvoiceLineItemPreset preset);
    Task<IReadOnlyList<InvoiceLineItemCollectionPreset>> GetLineItemCollectionPresetsAsync(CancellationToken cancellationToken = default);
    Task<InvoiceLineItemCollectionPreset?> GetLineItemCollectionPresetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> LineItemCollectionPresetLabelExistsAsync(string label, Guid? excludingId = null, CancellationToken cancellationToken = default);
    void AddLineItemCollectionPreset(InvoiceLineItemCollectionPreset preset);
    void RemoveLineItemCollectionPreset(InvoiceLineItemCollectionPreset preset);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
