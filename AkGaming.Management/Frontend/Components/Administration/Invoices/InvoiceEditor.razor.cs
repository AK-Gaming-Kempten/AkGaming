using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Text;

namespace AkGaming.Management.Frontend.Components.Administration.Invoices;

public partial class InvoiceEditor : ComponentBase
{
    [Inject] private InvoiceManagementApiClient InvoiceApi { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;

    [Parameter] public InvoiceDetailsDto Model { get; set; } = new();
    [Parameter] public IReadOnlyList<InvoicePartyPresetDto> Presets { get; set; } = [];
    [Parameter] public IReadOnlyList<InvoicePaymentTermsPresetDto> PaymentTermsPresets { get; set; } = [];
    [Parameter] public IReadOnlyList<InvoiceBankAccountPresetDto> BankAccountPresets { get; set; } = [];
    [Parameter] public IReadOnlyList<InvoiceLineItemPresetDto> LineItemPresets { get; set; } = [];
    [Parameter] public IReadOnlyList<InvoiceLineItemCollectionPresetDto> LineItemCollectionPresets { get; set; } = [];
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<InvoiceDetailsDto> OnSave { get; set; }
    [Parameter] public EventCallback<Guid> OnDelete { get; set; }
    [Parameter] public EventCallback<string?> OnError { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private string? _previewHtml;
    private decimal TotalAmount => Model.LineItems.Sum(item => item.TotalPrice);

    private Task SaveAsync(EditContext _)
    {
        return OnSave.InvokeAsync(Model);
    }

    private Task DeleteAsync()
    {
        return OnDelete.InvokeAsync(Model.Id);
    }

    private Task CancelAsync()
    {
        return OnCancel.InvokeAsync();
    }

    private void AddLineItem()
    {
        Model.LineItems.Add(new InvoiceLineItemDto { Quantity = 1m });
    }

    private void RemoveLineItem(InvoiceLineItemDto item)
    {
        if (Model.LineItems.Count > 1)
            Model.LineItems.Remove(item);
    }

    private void ApplyPaymentTermsPreset(ChangeEventArgs args)
    {
        if (!Guid.TryParse(args.Value?.ToString(), out var presetId))
            return;

        var preset = PaymentTermsPresets.FirstOrDefault(candidate => candidate.Id == presetId);
        if (preset is not null)
            Model.PaymentTerms = preset.Terms;
    }

    private void AddLineItemPreset(ChangeEventArgs args)
    {
        if (!Guid.TryParse(args.Value?.ToString(), out var presetId))
            return;
        var preset = LineItemPresets.FirstOrDefault(candidate => candidate.Id == presetId);
        if (preset is not null)
            Model.LineItems.Add(CloneLineItem(preset.LineItem));
    }

    private void ApplyLineItemCollectionPreset(ChangeEventArgs args)
    {
        if (!Guid.TryParse(args.Value?.ToString(), out var presetId))
            return;
        var preset = LineItemCollectionPresets.FirstOrDefault(candidate => candidate.Id == presetId);
        if (preset is null)
            return;
        Model.LineItems = preset.LineItems.Select(CloneLineItem).ToList();
    }

    private void ApplyBankAccountPreset(ChangeEventArgs args)
    {
        if (!Guid.TryParse(args.Value?.ToString(), out var presetId))
            return;
        var preset = BankAccountPresets.FirstOrDefault(candidate => candidate.Id == presetId);
        if (preset is null)
            return;
        Model.BankDetails.Iban = preset.BankDetails.Iban;
        Model.BankDetails.Bic = preset.BankDetails.Bic;
        Model.BankDetails.Blz = preset.BankDetails.Blz;
        Model.BankDetails.AccountHolder = preset.BankDetails.AccountHolder;
    }

    private static InvoiceLineItemDto CloneLineItem(InvoiceLineItemDto item) => new()
    {
        Description = item.Description,
        UnitPrice = item.UnitPrice,
        Quantity = item.Quantity
    };

    private async Task PreviewAsync()
    {
        await OnError.InvokeAsync(null);
        var result = await InvoiceApi.RenderHtmlAsync(Model);
        if (!result.IsSuccess)
        {
            await OnError.InvokeAsync(result.Error ?? "The invoice preview could not be generated.");
            return;
        }

        _previewHtml = result.Value;
    }

    private async Task DownloadPdfAsync()
    {
        await OnError.InvokeAsync(null);
        var result = await InvoiceApi.RenderPdfAsync(Model);
        if (!result.IsSuccess)
        {
            await OnError.InvokeAsync(result.Error ?? "The invoice PDF could not be generated.");
            return;
        }

        var fileName = $"invoice-{Model.InvoiceNumber}.pdf";
        var base64 = Convert.ToBase64String(result.Value!);
        await Js.InvokeVoidAsync("akGaming.downloadFile", fileName, "application/pdf", base64);
    }

    private async Task DownloadHtmlAsync()
    {
        await OnError.InvokeAsync(null);
        var result = await InvoiceApi.RenderHtmlAsync(Model);
        if (!result.IsSuccess)
        {
            await OnError.InvokeAsync(result.Error ?? "The invoice HTML could not be generated.");
            return;
        }

        var fileName = $"invoice-{Model.InvoiceNumber}.html";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(result.Value!));
        await Js.InvokeVoidAsync("akGaming.downloadFile", fileName, "text/html; charset=utf-8", base64);
    }

    private void ClosePreview()
    {
        _previewHtml = null;
    }
}
