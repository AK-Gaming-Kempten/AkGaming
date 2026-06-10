using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.Invoices;

public partial class InvoiceBankAccountPresetsPanel : ComponentBase
{
    [Inject] private InvoiceManagementApiClient Api { get; set; } = null!;
    private List<InvoiceBankAccountPresetDto>? _presets;
    private InvoiceBankAccountPresetDto _editing = new();
    private bool _showCreate;
    private bool _isBusy;
    private bool _isMobileDetailOpen;
    private string? _error;
    private string? _status;

    protected override Task OnInitializedAsync() => LoadAsync();
    private async Task LoadAsync() { _isBusy = true; var result = await Api.GetBankAccountPresetsAsync(); _isBusy = false; _presets = result.Value?.ToList() ?? []; _error = result.IsSuccess ? null : result.Error; }
    private void ToggleCreate() { _showCreate = !_showCreate; _editing = new(); _isMobileDetailOpen = _showCreate; ClearMessages(); }
    private void Select(InvoiceBankAccountPresetDto preset) { _showCreate = false; _editing = Clone(preset); _isMobileDetailOpen = true; ClearMessages(); }
    private async Task SaveAsync() { _isBusy = true; ClearMessages(); var result = _editing.Id == Guid.Empty ? await Api.CreateBankAccountPresetAsync(_editing) : await Api.UpdateBankAccountPresetAsync(_editing.Id, _editing); _isBusy = false; if (!result.IsSuccess) { _error = result.Error; return; } _editing = result.Value!; _showCreate = false; _status = $"Preset {_editing.Label} saved."; await LoadAsync(); }
    private async Task DeleteAsync() { _isBusy = true; ClearMessages(); var result = await Api.DeleteBankAccountPresetAsync(_editing.Id); _isBusy = false; if (!result.IsSuccess) { _error = result.Error; return; } _editing = new(); _isMobileDetailOpen = false; _status = "Preset deleted."; await LoadAsync(); }
    private RenderFragment Form(string title) => builder =>
    {
        builder.OpenElement(0, "section"); builder.AddAttribute(1, "class", "panel ui-stack-md");
        builder.OpenElement(2, "h4"); builder.AddAttribute(3, "class", "ui-title"); builder.AddContent(4, title); builder.CloseElement();
        builder.OpenComponent<BankAccountPresetForm>(5); builder.AddAttribute(6, nameof(BankAccountPresetForm.Model), _editing); builder.AddAttribute(7, nameof(BankAccountPresetForm.IsBusy), _isBusy); builder.AddAttribute(8, nameof(BankAccountPresetForm.ShowDelete), _editing.Id != Guid.Empty); builder.AddAttribute(9, nameof(BankAccountPresetForm.OnSave), EventCallback.Factory.Create(this, SaveAsync)); builder.AddAttribute(10, nameof(BankAccountPresetForm.OnDelete), EventCallback.Factory.Create(this, DeleteAsync)); builder.CloseComponent(); builder.CloseElement();
    };
    private void ClearMessages() { _error = null; _status = null; }
    private void ShowListMobile() { _showCreate = false; _editing = new(); _isMobileDetailOpen = false; ClearMessages(); }
    private static InvoiceBankAccountPresetDto Clone(InvoiceBankAccountPresetDto value) => new() { Id = value.Id, Label = value.Label, CreatedAt = value.CreatedAt, UpdatedAt = value.UpdatedAt, BankDetails = new() { Iban = value.BankDetails.Iban, Bic = value.BankDetails.Bic, Blz = value.BankDetails.Blz, AccountHolder = value.BankDetails.AccountHolder } };
}
