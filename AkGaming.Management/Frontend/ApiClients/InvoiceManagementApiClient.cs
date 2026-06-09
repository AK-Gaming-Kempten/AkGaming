using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;

namespace AkGaming.Management.Frontend.ApiClients;

public sealed class InvoiceManagementApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<Result<IReadOnlyList<InvoiceSummaryDto>>> GetInvoicesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<InvoiceSummaryDto>>("invoices", cancellationToken);

    public Task<Result<InvoiceDetailsDto>> GetInvoiceAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync<InvoiceDetailsDto>($"invoices/{id}", cancellationToken);

    public Task<Result<InvoiceDetailsDto>> CreateInvoiceAsync(InvoiceDetailsDto invoice, CancellationToken cancellationToken = default) =>
        PostJsonAsync<InvoiceDetailsDto, InvoiceDetailsDto>("invoices", invoice, cancellationToken);

    public Task<Result<InvoiceDetailsDto>> UpdateInvoiceAsync(Guid id, InvoiceDetailsDto invoice, CancellationToken cancellationToken = default) =>
        PutJsonAsync<InvoiceDetailsDto, InvoiceDetailsDto>($"invoices/{id}", invoice, cancellationToken);

    public Task<Result> DeleteInvoiceAsync(Guid id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"invoices/{id}", cancellationToken);

    public Task<Result<string>> RenderHtmlAsync(InvoiceDetailsDto invoice, CancellationToken cancellationToken = default) =>
        PostForStringAsync("invoices/render-html", invoice, cancellationToken);

    public Task<Result<byte[]>> RenderPdfAsync(InvoiceDetailsDto invoice, CancellationToken cancellationToken = default) =>
        PostForBytesAsync("invoices/render-pdf", invoice, cancellationToken);

    public Task<Result<IReadOnlyList<InvoicePartyPresetDto>>> GetPartyPresetsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<InvoicePartyPresetDto>>("invoice-party-presets", cancellationToken);

    public Task<Result<InvoicePartyPresetDto>> CreatePartyPresetAsync(InvoicePartyPresetDto preset, CancellationToken cancellationToken = default) =>
        PostJsonAsync<InvoicePartyPresetDto, InvoicePartyPresetDto>("invoice-party-presets", preset, cancellationToken);

    public Task<Result<InvoicePartyPresetDto>> UpdatePartyPresetAsync(Guid id, InvoicePartyPresetDto preset, CancellationToken cancellationToken = default) =>
        PutJsonAsync<InvoicePartyPresetDto, InvoicePartyPresetDto>($"invoice-party-presets/{id}", preset, cancellationToken);

    public Task<Result> DeletePartyPresetAsync(Guid id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"invoice-party-presets/{id}", cancellationToken);

    public Task<Result<IReadOnlyList<InvoicePaymentTermsPresetDto>>> GetPaymentTermsPresetsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<InvoicePaymentTermsPresetDto>>("invoice-payment-terms-presets", cancellationToken);

    public Task<Result<InvoicePaymentTermsPresetDto>> CreatePaymentTermsPresetAsync(InvoicePaymentTermsPresetDto preset, CancellationToken cancellationToken = default) =>
        PostJsonAsync<InvoicePaymentTermsPresetDto, InvoicePaymentTermsPresetDto>("invoice-payment-terms-presets", preset, cancellationToken);

    public Task<Result<InvoicePaymentTermsPresetDto>> UpdatePaymentTermsPresetAsync(Guid id, InvoicePaymentTermsPresetDto preset, CancellationToken cancellationToken = default) =>
        PutJsonAsync<InvoicePaymentTermsPresetDto, InvoicePaymentTermsPresetDto>($"invoice-payment-terms-presets/{id}", preset, cancellationToken);

    public Task<Result> DeletePaymentTermsPresetAsync(Guid id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"invoice-payment-terms-presets/{id}", cancellationToken);

    public Task<Result<IReadOnlyList<InvoiceBankAccountPresetDto>>> GetBankAccountPresetsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<InvoiceBankAccountPresetDto>>("invoice-bank-account-presets", cancellationToken);
    public Task<Result<InvoiceBankAccountPresetDto>> CreateBankAccountPresetAsync(InvoiceBankAccountPresetDto preset, CancellationToken cancellationToken = default) =>
        PostJsonAsync<InvoiceBankAccountPresetDto, InvoiceBankAccountPresetDto>("invoice-bank-account-presets", preset, cancellationToken);
    public Task<Result<InvoiceBankAccountPresetDto>> UpdateBankAccountPresetAsync(Guid id, InvoiceBankAccountPresetDto preset, CancellationToken cancellationToken = default) =>
        PutJsonAsync<InvoiceBankAccountPresetDto, InvoiceBankAccountPresetDto>($"invoice-bank-account-presets/{id}", preset, cancellationToken);
    public Task<Result> DeleteBankAccountPresetAsync(Guid id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"invoice-bank-account-presets/{id}", cancellationToken);

    public Task<Result<IReadOnlyList<InvoiceLineItemPresetDto>>> GetLineItemPresetsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<InvoiceLineItemPresetDto>>("invoice-line-item-presets", cancellationToken);
    public Task<Result<InvoiceLineItemPresetDto>> CreateLineItemPresetAsync(InvoiceLineItemPresetDto preset, CancellationToken cancellationToken = default) =>
        PostJsonAsync<InvoiceLineItemPresetDto, InvoiceLineItemPresetDto>("invoice-line-item-presets", preset, cancellationToken);
    public Task<Result<InvoiceLineItemPresetDto>> UpdateLineItemPresetAsync(Guid id, InvoiceLineItemPresetDto preset, CancellationToken cancellationToken = default) =>
        PutJsonAsync<InvoiceLineItemPresetDto, InvoiceLineItemPresetDto>($"invoice-line-item-presets/{id}", preset, cancellationToken);
    public Task<Result> DeleteLineItemPresetAsync(Guid id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"invoice-line-item-presets/{id}", cancellationToken);

    public Task<Result<IReadOnlyList<InvoiceLineItemCollectionPresetDto>>> GetLineItemCollectionPresetsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<InvoiceLineItemCollectionPresetDto>>("invoice-line-item-collection-presets", cancellationToken);
    public Task<Result<InvoiceLineItemCollectionPresetDto>> CreateLineItemCollectionPresetAsync(InvoiceLineItemCollectionPresetDto preset, CancellationToken cancellationToken = default) =>
        PostJsonAsync<InvoiceLineItemCollectionPresetDto, InvoiceLineItemCollectionPresetDto>("invoice-line-item-collection-presets", preset, cancellationToken);
    public Task<Result<InvoiceLineItemCollectionPresetDto>> UpdateLineItemCollectionPresetAsync(Guid id, InvoiceLineItemCollectionPresetDto preset, CancellationToken cancellationToken = default) =>
        PutJsonAsync<InvoiceLineItemCollectionPresetDto, InvoiceLineItemCollectionPresetDto>($"invoice-line-item-collection-presets/{id}", preset, cancellationToken);
    public Task<Result> DeleteLineItemCollectionPresetAsync(Guid id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"invoice-line-item-collection-presets/{id}", cancellationToken);
}
