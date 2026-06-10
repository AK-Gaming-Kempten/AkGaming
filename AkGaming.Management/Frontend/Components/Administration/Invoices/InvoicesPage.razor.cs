using AkGaming.Core.Constants;
using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.Invoices;

public partial class InvoicesPage : ComponentBase
{
    [Inject] private InvoiceManagementApiClient InvoiceApi { get; set; } = null!;

    private List<InvoiceSummaryDto>? _invoices;
    private IReadOnlyList<InvoicePartyPresetDto> _presets = [];
    private IReadOnlyList<InvoicePaymentTermsPresetDto> _paymentTermsPresets = [];
    private IReadOnlyList<InvoiceBankAccountPresetDto> _bankAccountPresets = [];
    private IReadOnlyList<InvoiceLineItemPresetDto> _lineItemPresets = [];
    private IReadOnlyList<InvoiceLineItemCollectionPresetDto> _lineItemCollectionPresets = [];
    private InvoiceDetailsDto? _selectedInvoice;
    private bool _isBusy;
    private bool _isMobileDetailOpen;
    private string? _error;
    private string? _status;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _isBusy = true;
        _error = null;

        var invoicesResult = await InvoiceApi.GetInvoicesAsync();
        var presetsResult = await InvoiceApi.GetPartyPresetsAsync();
        var paymentTermsPresetsResult = await InvoiceApi.GetPaymentTermsPresetsAsync();
        var bankAccountPresetsResult = await InvoiceApi.GetBankAccountPresetsAsync();
        var lineItemPresetsResult = await InvoiceApi.GetLineItemPresetsAsync();
        var lineItemCollectionPresetsResult = await InvoiceApi.GetLineItemCollectionPresetsAsync();

        _isBusy = false;
        if (!invoicesResult.IsSuccess)
        {
            _invoices = [];
            _error = invoicesResult.Error;
            return;
        }

        _invoices = invoicesResult.Value?.ToList() ?? [];
        if (presetsResult.IsSuccess)
            _presets = presetsResult.Value ?? [];
        else
            _error = presetsResult.Error;

        if (paymentTermsPresetsResult.IsSuccess)
            _paymentTermsPresets = paymentTermsPresetsResult.Value ?? [];
        else
            _error = paymentTermsPresetsResult.Error;

        if (bankAccountPresetsResult.IsSuccess)
            _bankAccountPresets = bankAccountPresetsResult.Value ?? [];
        else
            _error = bankAccountPresetsResult.Error;
        if (lineItemPresetsResult.IsSuccess)
            _lineItemPresets = lineItemPresetsResult.Value ?? [];
        else
            _error = lineItemPresetsResult.Error;
        if (lineItemCollectionPresetsResult.IsSuccess)
            _lineItemCollectionPresets = lineItemCollectionPresetsResult.Value ?? [];
        else
            _error = lineItemCollectionPresetsResult.Error;
    }

    private async Task ReloadAsync()
    {
        _status = null;
        await LoadAsync();
    }

    private async Task SelectInvoiceAsync(Guid id)
    {
        _isBusy = true;
        _error = null;
        _status = null;
        var result = await InvoiceApi.GetInvoiceAsync(id);
        _isBusy = false;

        if (!result.IsSuccess)
        {
            _error = result.Error;
            return;
        }

        _selectedInvoice = result.Value;
        _isMobileDetailOpen = true;
    }

    private void StartCreate()
    {
        _error = null;
        _status = null;
        _selectedInvoice = new InvoiceDetailsDto
        {
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
            SignatureName = ClubConstants.Organization.LegalName,
            LineItems = [new InvoiceLineItemDto { Quantity = 1m }]
        };
        _isMobileDetailOpen = true;
    }

    private async Task SaveAsync(InvoiceDetailsDto invoice)
    {
        _isBusy = true;
        _error = null;
        _status = null;

        var result = invoice.Id == Guid.Empty
            ? await InvoiceApi.CreateInvoiceAsync(invoice)
            : await InvoiceApi.UpdateInvoiceAsync(invoice.Id, invoice);

        _isBusy = false;
        if (!result.IsSuccess)
        {
            _error = result.Error;
            return;
        }

        _selectedInvoice = result.Value;
        _status = $"Invoice {_selectedInvoice!.InvoiceNumber} saved.";
        await LoadInvoiceListAsync();
    }

    private async Task DeleteAsync(Guid id)
    {
        _isBusy = true;
        _error = null;
        _status = null;
        var result = await InvoiceApi.DeleteInvoiceAsync(id);
        _isBusy = false;

        if (!result.IsSuccess)
        {
            _error = result.Error;
            return;
        }

        _selectedInvoice = null;
        _isMobileDetailOpen = false;
        _status = "Invoice deleted.";
        await LoadInvoiceListAsync();
    }

    private async Task LoadInvoiceListAsync()
    {
        var result = await InvoiceApi.GetInvoicesAsync();
        if (result.IsSuccess)
            _invoices = result.Value?.ToList() ?? [];
        else
            _error = result.Error;
    }

    private void CloseEditor()
    {
        _selectedInvoice = null;
        _error = null;
        _isMobileDetailOpen = false;
    }

    private void ShowListMobile()
    {
        _selectedInvoice = null;
        _isMobileDetailOpen = false;
    }

    private void ShowError(string? message)
    {
        _error = message;
    }

    private void DismissError()
    {
        _error = null;
    }
}
