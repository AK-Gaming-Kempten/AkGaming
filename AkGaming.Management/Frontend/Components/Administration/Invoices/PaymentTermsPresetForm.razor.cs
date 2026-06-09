using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.Invoices;

public partial class PaymentTermsPresetForm : ComponentBase
{
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public InvoicePaymentTermsPresetDto Model { get; set; } = new();
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public bool ShowDelete { get; set; }
    [Parameter] public EventCallback OnSave { get; set; }
    [Parameter] public EventCallback OnDelete { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private Task SaveAsync() => OnSave.InvokeAsync();
    private Task DeleteAsync() => OnDelete.InvokeAsync();
    private Task CancelAsync() => OnCancel.InvokeAsync();
}
