using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;

namespace AkGaming.Management.Modules.InvoiceManagement.Contracts.Services;

public interface IInvoiceManagementService
{
    Task<Result<IReadOnlyList<InvoiceSummaryDto>>> GetInvoicesAsync(CancellationToken cancellationToken = default);
    Task<Result<InvoiceDetailsDto>> GetInvoiceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<InvoiceDetailsDto>> CreateInvoiceAsync(InvoiceDetailsDto request, CancellationToken cancellationToken = default);
    Task<Result<InvoiceDetailsDto>> UpdateInvoiceAsync(Guid id, InvoiceDetailsDto request, CancellationToken cancellationToken = default);
    Task<Result> DeleteInvoiceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<string>> RenderHtmlAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<byte[]>> RenderPdfAsync(Guid id, CancellationToken cancellationToken = default);
    Result<string> RenderHtml(InvoiceDetailsDto request);
    Result<byte[]> RenderPdf(InvoiceDetailsDto request);
    Task<Result<IReadOnlyList<InvoicePartyPresetDto>>> GetPartyPresetsAsync(CancellationToken cancellationToken = default);
    Task<Result<InvoicePartyPresetDto>> CreatePartyPresetAsync(InvoicePartyPresetDto request, CancellationToken cancellationToken = default);
    Task<Result<InvoicePartyPresetDto>> UpdatePartyPresetAsync(Guid id, InvoicePartyPresetDto request, CancellationToken cancellationToken = default);
    Task<Result> DeletePartyPresetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<InvoicePaymentTermsPresetDto>>> GetPaymentTermsPresetsAsync(CancellationToken cancellationToken = default);
    Task<Result<InvoicePaymentTermsPresetDto>> CreatePaymentTermsPresetAsync(InvoicePaymentTermsPresetDto request, CancellationToken cancellationToken = default);
    Task<Result<InvoicePaymentTermsPresetDto>> UpdatePaymentTermsPresetAsync(Guid id, InvoicePaymentTermsPresetDto request, CancellationToken cancellationToken = default);
    Task<Result> DeletePaymentTermsPresetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<InvoiceBankAccountPresetDto>>> GetBankAccountPresetsAsync(CancellationToken cancellationToken = default);
    Task<Result<InvoiceBankAccountPresetDto>> CreateBankAccountPresetAsync(InvoiceBankAccountPresetDto request, CancellationToken cancellationToken = default);
    Task<Result<InvoiceBankAccountPresetDto>> UpdateBankAccountPresetAsync(Guid id, InvoiceBankAccountPresetDto request, CancellationToken cancellationToken = default);
    Task<Result> DeleteBankAccountPresetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<InvoiceLineItemPresetDto>>> GetLineItemPresetsAsync(CancellationToken cancellationToken = default);
    Task<Result<InvoiceLineItemPresetDto>> CreateLineItemPresetAsync(InvoiceLineItemPresetDto request, CancellationToken cancellationToken = default);
    Task<Result<InvoiceLineItemPresetDto>> UpdateLineItemPresetAsync(Guid id, InvoiceLineItemPresetDto request, CancellationToken cancellationToken = default);
    Task<Result> DeleteLineItemPresetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<InvoiceLineItemCollectionPresetDto>>> GetLineItemCollectionPresetsAsync(CancellationToken cancellationToken = default);
    Task<Result<InvoiceLineItemCollectionPresetDto>> CreateLineItemCollectionPresetAsync(InvoiceLineItemCollectionPresetDto request, CancellationToken cancellationToken = default);
    Task<Result<InvoiceLineItemCollectionPresetDto>> UpdateLineItemCollectionPresetAsync(Guid id, InvoiceLineItemCollectionPresetDto request, CancellationToken cancellationToken = default);
    Task<Result> DeleteLineItemCollectionPresetAsync(Guid id, CancellationToken cancellationToken = default);
}
