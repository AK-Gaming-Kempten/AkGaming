using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Management.Frontend.Components.Administration.Invoices;
public partial class InvoiceLineItemPresetsPanel : ComponentBase
{
    [Inject] private InvoiceManagementApiClient Api { get; set; } = null!;
    private List<InvoiceLineItemPresetDto>? _presets; private InvoiceLineItemPresetDto _editing = new() { LineItem = new() { Quantity = 1m } }; private bool _showCreate; private bool _isBusy; private string? _error; private string? _status;
    protected override Task OnInitializedAsync() => LoadAsync();
    private async Task LoadAsync() { _isBusy = true; var r = await Api.GetLineItemPresetsAsync(); _isBusy = false; _presets = r.Value?.ToList() ?? []; _error = r.IsSuccess ? null : r.Error; }
    private void ToggleCreate() { _showCreate = !_showCreate; _editing = New(); Clear(); }
    private void Select(InvoiceLineItemPresetDto p) { _showCreate = false; _editing = Clone(p); Clear(); }
    private async Task SaveAsync() { _isBusy = true; Clear(); var r = _editing.Id == Guid.Empty ? await Api.CreateLineItemPresetAsync(_editing) : await Api.UpdateLineItemPresetAsync(_editing.Id, _editing); _isBusy = false; if (!r.IsSuccess) { _error = r.Error; return; } _editing = r.Value!; _showCreate = false; _status = $"Preset {_editing.Label} saved."; await LoadAsync(); }
    private async Task DeleteAsync() { _isBusy = true; Clear(); var r = await Api.DeleteLineItemPresetAsync(_editing.Id); _isBusy = false; if (!r.IsSuccess) { _error = r.Error; return; } _editing = New(); _status = "Preset deleted."; await LoadAsync(); }
    private RenderFragment Form(string title) => b => { b.OpenComponent<LineItemPresetForm>(0); b.AddAttribute(1, nameof(LineItemPresetForm.Title), title); b.AddAttribute(2, nameof(LineItemPresetForm.Model), _editing); b.AddAttribute(3, nameof(LineItemPresetForm.IsBusy), _isBusy); b.AddAttribute(4, nameof(LineItemPresetForm.ShowDelete), _editing.Id != Guid.Empty); b.AddAttribute(5, nameof(LineItemPresetForm.OnSave), EventCallback.Factory.Create(this, SaveAsync)); b.AddAttribute(6, nameof(LineItemPresetForm.OnDelete), EventCallback.Factory.Create(this, DeleteAsync)); b.CloseComponent(); };
    private void Clear() { _error = null; _status = null; }
    private static InvoiceLineItemPresetDto New() => new() { LineItem = new() { Quantity = 1m } };
    private static InvoiceLineItemPresetDto Clone(InvoiceLineItemPresetDto p) => new() { Id = p.Id, Label = p.Label, CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt, LineItem = new() { Description = p.LineItem.Description, UnitPrice = p.LineItem.UnitPrice, Quantity = p.LineItem.Quantity } };
}
