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
    private DiscordGuildCatalogDto? _discordCatalog;
    private bool _busy;
    private string? _error;
    private string? _dialogError;
    protected override async Task OnParametersSetAsync() => await LoadAsync();
    private async Task LoadAsync() { var result = await Api.GetEventAsync(EventId); if (result.IsSuccess) _event = result.Value; else _error = result.Error; }
    private async Task OpenCreateDialogAsync()
    {
        var loadingCatalog = new DiscordGuildCatalogDto();
        _discordCatalog = loadingCatalog;
        _dialogError = null;
        StateHasChanged();

        var result = await Api.GetDiscordCatalogAsync();
        if (!ReferenceEquals(_discordCatalog, loadingCatalog))
            return;

        if (result.IsSuccess)
            _discordCatalog = result.Value ?? loadingCatalog;
        else
            _dialogError = Text["Allocation_DiscordUnavailable"];
    }
    private async Task CreateAllocationAsync(SaveAllocationRequest request) { _busy = true; _dialogError = null; var result = await Api.CreateAllocationAsync(EventId, request); _busy = false; if (!result.IsSuccess) { _dialogError = result.Error; return; } _discordCatalog = null; await LoadAsync(); }
    private async Task SetStatusAsync(AllocationApplicationDto application, AllocationApplicationStatus status) { _busy = true; var result = await Api.UpdateApplicationStatusAsync(application.Id, new UpdateAllocationApplicationStatusRequest { Status = status }); _busy = false; if (!result.IsSuccess) _error = result.Error; else await LoadAsync(); }
    private string ShareUrl(AllocationDto allocation) => Navigation.ToAbsoluteUri($"disbursements/claim/{allocation.ShareToken}").ToString();
    private Task CopyAsync(string value) => Js.InvokeVoidAsync("navigator.clipboard.writeText", value).AsTask();
    private static decimal Progress(AllocationDto allocation) => allocation.Amount <= 0 ? 0 : Math.Min(100, allocation.AppliedAmount / allocation.Amount * 100);
    private static decimal AvailableAmount(AllocationDto allocation) => Math.Max(0, allocation.Amount - allocation.AppliedAmount);
    private void ShowPaymentDetails(PaymentMethodSnapshotDto paymentMethod) => _selectedPaymentMethod = paymentMethod;
    private void HidePaymentDetails() => _selectedPaymentMethod = null;
    private void CloseCreateDialog() { if (!_busy) _discordCatalog = null; }
}
