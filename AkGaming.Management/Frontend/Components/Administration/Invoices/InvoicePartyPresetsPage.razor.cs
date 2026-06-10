using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.Invoices;

public partial class InvoicePartyPresetsPage : ComponentBase
{
    [Inject] private InvoiceManagementApiClient InvoiceApi { get; set; } = null!;

    private List<InvoicePartyPresetDto>? _presets;
    private InvoicePartyPresetDto _editingPreset = new();
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
        var result = await InvoiceApi.GetPartyPresetsAsync();
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
        _editingPreset = new InvoicePartyPresetDto();
        _isMobileDetailOpen = _showCreate;
        _error = null;
        _status = null;
    }

    private void SelectPreset(InvoicePartyPresetDto preset)
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
            ? await InvoiceApi.CreatePartyPresetAsync(_editingPreset)
            : await InvoiceApi.UpdatePartyPresetAsync(_editingPreset.Id, _editingPreset);
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
        var result = await InvoiceApi.DeletePartyPresetAsync(_editingPreset.Id);
        _isBusy = false;

        if (!result.IsSuccess)
        {
            _error = result.Error;
            return;
        }

        _editingPreset = new InvoicePartyPresetDto();
        _isMobileDetailOpen = false;
        _status = "Preset deleted.";
        await LoadAsync();
    }

    private static InvoicePartyPresetDto Clone(InvoicePartyPresetDto preset)
    {
        return new InvoicePartyPresetDto
        {
            Id = preset.Id,
            Label = preset.Label,
            Party = new InvoicePartyDto
            {
                Name = preset.Party.Name,
                Street = preset.Party.Street,
                PostalCode = preset.Party.PostalCode,
                City = preset.Party.City,
                Country = preset.Party.Country
            },
            CreatedAt = preset.CreatedAt,
            UpdatedAt = preset.UpdatedAt
        };
    }

    private void ShowListMobile()
    {
        _showCreate = false;
        _editingPreset = new InvoicePartyPresetDto();
        _isMobileDetailOpen = false;
    }
}
