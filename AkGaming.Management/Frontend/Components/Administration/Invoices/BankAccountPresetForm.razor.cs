using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Management.Frontend.Components.Administration.Invoices;
public partial class BankAccountPresetForm : ComponentBase
{
    [Parameter] public InvoiceBankAccountPresetDto Model { get; set; } = new();
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public bool ShowDelete { get; set; }
    [Parameter] public EventCallback OnSave { get; set; }
    [Parameter] public EventCallback OnDelete { get; set; }
    private Task SaveAsync() => OnSave.InvokeAsync();
    private Task DeleteAsync() => OnDelete.InvokeAsync();
}
