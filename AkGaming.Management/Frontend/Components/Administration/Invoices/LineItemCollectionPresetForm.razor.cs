using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO; using Microsoft.AspNetCore.Components;
namespace AkGaming.Management.Frontend.Components.Administration.Invoices;
public partial class LineItemCollectionPresetForm : ComponentBase
{
    [Parameter] public string Title { get; set; } = string.Empty; [Parameter] public InvoiceLineItemCollectionPresetDto Model { get; set; } = new(); [Parameter] public bool IsBusy { get; set; } [Parameter] public bool ShowDelete { get; set; } [Parameter] public EventCallback OnSave { get; set; } [Parameter] public EventCallback OnDelete { get; set; }
    private void AddItem() => Model.LineItems.Add(new InvoiceLineItemDto { Quantity = 1m });
    private void RemoveItem(InvoiceLineItemDto item) { if (Model.LineItems.Count > 1) Model.LineItems.Remove(item); }
    private Task SaveAsync() => OnSave.InvokeAsync(); private Task DeleteAsync() => OnDelete.InvokeAsync();
}
