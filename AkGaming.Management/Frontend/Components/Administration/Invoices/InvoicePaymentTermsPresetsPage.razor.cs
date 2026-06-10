using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.Invoices;

public partial class InvoicePaymentTermsPresetsPage : ComponentBase
{
    [Inject] private InvoiceManagementApiClient InvoiceApi { get; set; } = null!;

    private List<InvoicePaymentTermsPresetDto>? _presets;
    private InvoicePaymentTermsPresetDto _editingPreset = new();
    private bool _showCreate;
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
        var result = await InvoiceApi.GetPaymentTermsPresetsAsync();
        _isBusy = false;
        if (!result.IsSuccess)
        {
            _presets = [];
            _error = result.Error;
            return;
        }

        _presets = result.Value?.ToList() ?? [];
    }

    private void ToggleCreate()
    {
        _showCreate = !_showCreate;
        _editingPreset = new InvoicePaymentTermsPresetDto();
        _isMobileDetailOpen = _showCreate;
        _error = null;
        _status = null;
    }

    private void SelectPreset(InvoicePaymentTermsPresetDto preset)
    {
        _showCreate = false;
        _editingPreset = Clone(preset);
        _isMobileDetailOpen = true;
        _error = null;
        _status = null;
    }

    private async Task SaveAsync()
    {
        _isBusy = true;
        _error = null;
        _status = null;
        var result = _editingPreset.Id == Guid.Empty
            ? await InvoiceApi.CreatePaymentTermsPresetAsync(_editingPreset)
            : await InvoiceApi.UpdatePaymentTermsPresetAsync(_editingPreset.Id, _editingPreset);
        _isBusy = false;

        if (!result.IsSuccess)
        {
            _error = result.Error;
            return;
        }

        _editingPreset = result.Value!;
        _showCreate = false;
        _status = $"Preset {_editingPreset.Label} saved.";
        await LoadAsync();
    }

    private async Task DeleteAsync()
    {
        _isBusy = true;
        _error = null;
        _status = null;
        var result = await InvoiceApi.DeletePaymentTermsPresetAsync(_editingPreset.Id);
        _isBusy = false;

        if (!result.IsSuccess)
        {
            _error = result.Error;
            return;
        }

        _editingPreset = new InvoicePaymentTermsPresetDto();
        _isMobileDetailOpen = false;
        _status = "Preset deleted.";
        await LoadAsync();
    }

    private static InvoicePaymentTermsPresetDto Clone(InvoicePaymentTermsPresetDto preset)
    {
        return new InvoicePaymentTermsPresetDto
        {
            Id = preset.Id,
            Label = preset.Label,
            Terms = preset.Terms,
            CreatedAt = preset.CreatedAt,
            UpdatedAt = preset.UpdatedAt
        };
    }

    private static string GetPreview(string terms)
    {
        const int maxLength = 90;
        var normalized = terms.ReplaceLineEndings(" ").Trim();
        return normalized.Length <= maxLength ? normalized : $"{normalized[..maxLength]}...";
    }

    private void ShowListMobile()
    {
        _showCreate = false;
        _editingPreset = new InvoicePaymentTermsPresetDto();
        _isMobileDetailOpen = false;
    }
}
