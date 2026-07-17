using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.Disbursements.Contracts.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AkGaming.Management.Frontend.Components.Administration.Disbursements;

public partial class DisbursementEventPage : ComponentBase
{
    [Parameter] public Guid EventId { get; set; }
    [Inject] private DisbursementsApiClient Api { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;
    private DisbursementEventDto? _event;
    private PaymentMethodSnapshotDto? _selectedPaymentMethod;
    private SaveAllocationRequest _newAllocation = new();
    private bool _showCreate;
    private bool _busy;
    private string? _error;
    protected override async Task OnParametersSetAsync() => await LoadAsync();
    private async Task LoadAsync() { var result = await Api.GetEventAsync(EventId); if (result.IsSuccess) _event = result.Value; else _error = result.Error; }
    private async Task CreateAllocationAsync() { _busy = true; var result = await Api.CreateAllocationAsync(EventId, _newAllocation); _busy = false; if (!result.IsSuccess) { _error = result.Error; return; } _newAllocation = new(); _showCreate = false; await LoadAsync(); }
    private async Task SetStatusAsync(AllocationApplicationDto application, AllocationApplicationStatus status) { _busy = true; var result = await Api.UpdateApplicationStatusAsync(application.Id, new UpdateAllocationApplicationStatusRequest { Status = status }); _busy = false; if (!result.IsSuccess) _error = result.Error; else await LoadAsync(); }
    private string ShareUrl(AllocationDto allocation) => Navigation.ToAbsoluteUri($"disbursements/claim/{allocation.ShareToken}").ToString();
    private Task CopyAsync(string value) => Js.InvokeVoidAsync("navigator.clipboard.writeText", value).AsTask();
    private static decimal Progress(AllocationDto allocation) => allocation.Amount <= 0 ? 0 : Math.Min(100, allocation.AppliedAmount / allocation.Amount * 100);
    private static decimal AvailableAmount(AllocationDto allocation) => Math.Max(0, allocation.Amount - allocation.AppliedAmount);
    private void ShowPaymentDetails(PaymentMethodSnapshotDto paymentMethod) => _selectedPaymentMethod = paymentMethod;
    private void HidePaymentDetails() => _selectedPaymentMethod = null;
}
