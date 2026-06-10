using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Management.Frontend.Components.Administration.Invoices;
public partial class InvoiceLineItemCollectionPresetsPanel : ComponentBase
{
    [Inject] private InvoiceManagementApiClient Api { get; set; } = null!;
    private List<InvoiceLineItemCollectionPresetDto>? _presets; private InvoiceLineItemCollectionPresetDto _editing = New(); private bool _showCreate; private bool _isBusy; private bool _isMobileDetailOpen; private string? _error; private string? _status;
    protected override Task OnInitializedAsync() => LoadAsync();
    private async Task LoadAsync() { _isBusy = true; var r = await Api.GetLineItemCollectionPresetsAsync(); _isBusy = false; _presets = r.Value?.ToList() ?? []; _error = r.IsSuccess ? null : r.Error; }
    private void ToggleCreate() { _showCreate = !_showCreate; _editing = New(); _isMobileDetailOpen = _showCreate; Clear(); }
    private void Select(InvoiceLineItemCollectionPresetDto p) { _showCreate = false; _editing = Clone(p); _isMobileDetailOpen = true; Clear(); }
    private async Task SaveAsync() { _isBusy = true; Clear(); var r = _editing.Id == Guid.Empty ? await Api.CreateLineItemCollectionPresetAsync(_editing) : await Api.UpdateLineItemCollectionPresetAsync(_editing.Id, _editing); _isBusy = false; if (!r.IsSuccess) { _error = r.Error; return; } _editing = r.Value!; _showCreate = false; _status = $"Collection {_editing.Label} saved."; await LoadAsync(); }
    private async Task DeleteAsync() { _isBusy = true; Clear(); var r = await Api.DeleteLineItemCollectionPresetAsync(_editing.Id); _isBusy = false; if (!r.IsSuccess) { _error = r.Error; return; } _editing = New(); _isMobileDetailOpen = false; _status = "Collection deleted."; await LoadAsync(); }
    private RenderFragment Form(string title) => b => { b.OpenComponent<LineItemCollectionPresetForm>(0); b.AddAttribute(1, nameof(LineItemCollectionPresetForm.Title), title); b.AddAttribute(2, nameof(LineItemCollectionPresetForm.Model), _editing); b.AddAttribute(3, nameof(LineItemCollectionPresetForm.IsBusy), _isBusy); b.AddAttribute(4, nameof(LineItemCollectionPresetForm.ShowDelete), _editing.Id != Guid.Empty); b.AddAttribute(5, nameof(LineItemCollectionPresetForm.OnSave), EventCallback.Factory.Create(this, SaveAsync)); b.AddAttribute(6, nameof(LineItemCollectionPresetForm.OnDelete), EventCallback.Factory.Create(this, DeleteAsync)); b.CloseComponent(); };
    private void Clear() { _error = null; _status = null; }
    private void ShowListMobile() { _showCreate = false; _editing = New(); _isMobileDetailOpen = false; Clear(); }
    private static InvoiceLineItemCollectionPresetDto New() => new() { LineItems = [new() { Quantity = 1m }] };
    private static InvoiceLineItemCollectionPresetDto Clone(InvoiceLineItemCollectionPresetDto p) => new() { Id = p.Id, Label = p.Label, CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt, LineItems = p.LineItems.Select(i => new InvoiceLineItemDto { Description = i.Description, UnitPrice = i.UnitPrice, Quantity = i.Quantity }).ToList() };
}
