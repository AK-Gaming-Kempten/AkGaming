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
    private AllocationDto? _editingAllocation;
    private bool _discordCatalogReady;
    private bool _busy;
    private string? _error;
    private string? _dialogError;
    protected override async Task OnParametersSetAsync() => await LoadAsync();
    private async Task LoadAsync() { var result = await Api.GetEventAsync(EventId); if (result.IsSuccess) _event = result.Value; else _error = result.Error; }
    private Task OpenCreateDialogAsync()
    {
        return OpenAllocationDialogAsync(null);
    }

    private Task OpenEditDialogAsync(AllocationDto allocation)
    {
        return OpenAllocationDialogAsync(allocation);
    }

    private async Task OpenAllocationDialogAsync(AllocationDto? allocation)
    {
        _editingAllocation = allocation;
        var loadingCatalog = CatalogWithExistingRoute(new DiscordGuildCatalogDto(), allocation);
        _discordCatalog = loadingCatalog;
        _discordCatalogReady = false;
        _dialogError = null;
        StateHasChanged();

        var result = await Api.GetDiscordCatalogAsync();
        if (!ReferenceEquals(_discordCatalog, loadingCatalog))
            return;

        if (result.IsSuccess)
            _discordCatalog = CatalogWithExistingRoute(result.Value ?? loadingCatalog, allocation);
        else
            _dialogError = Text["Allocation_DiscordUnavailable"];
        _discordCatalogReady = true;
    }

    private async Task SaveAllocationAsync(SaveAllocationRequest request)
    {
        _busy = true;
        _dialogError = null;
        var result = _editingAllocation is null
            ? await Api.CreateAllocationAsync(EventId, request)
            : await Api.UpdateAllocationAsync(_editingAllocation.Id, request);
        _busy = false;
        if (!result.IsSuccess)
        {
            _dialogError = result.Error;
            return;
        }

        _discordCatalog = null;
        _editingAllocation = null;
        _discordCatalogReady = false;
        await LoadAsync();
    }
    private async Task SetStatusAsync(AllocationApplicationDto application, AllocationApplicationStatus status) { _busy = true; var result = await Api.UpdateApplicationStatusAsync(application.Id, new UpdateAllocationApplicationStatusRequest { Status = status }); _busy = false; if (!result.IsSuccess) _error = result.Error; else await LoadAsync(); }
    private string ShareUrl(AllocationDto allocation) => Navigation.ToAbsoluteUri($"disbursements/claim/{allocation.ShareToken}").ToString();
    private Task CopyAsync(string value) => Js.InvokeVoidAsync("navigator.clipboard.writeText", value).AsTask();
    private static decimal Progress(AllocationDto allocation) => allocation.Amount <= 0 ? 0 : Math.Min(100, allocation.AppliedAmount / allocation.Amount * 100);
    private static decimal AvailableAmount(AllocationDto allocation) => Math.Max(0, allocation.Amount - allocation.AppliedAmount);
    private void ShowPaymentDetails(PaymentMethodSnapshotDto paymentMethod) => _selectedPaymentMethod = paymentMethod;
    private void HidePaymentDetails() => _selectedPaymentMethod = null;
    private void CloseAllocationDialog()
    {
        if (_busy)
            return;
        _discordCatalog = null;
        _editingAllocation = null;
        _discordCatalogReady = false;
    }

    private static DiscordGuildCatalogDto CatalogWithExistingRoute(
        DiscordGuildCatalogDto catalog,
        AllocationDto? allocation)
    {
        if (allocation is null)
            return catalog;
        if (!string.IsNullOrWhiteSpace(allocation.DiscordChannelId)
            && catalog.Channels.All(channel => channel.Id != allocation.DiscordChannelId))
        {
            catalog.Channels.Add(new DiscordChannelDto
            {
                Id = allocation.DiscordChannelId,
                Name = allocation.DiscordChannelName
            });
        }
        if (!string.IsNullOrWhiteSpace(allocation.DiscordRoleId)
            && catalog.Roles.All(role => role.Id != allocation.DiscordRoleId))
        {
            catalog.Roles.Add(new DiscordRoleDto
            {
                Id = allocation.DiscordRoleId,
                Name = allocation.DiscordRoleName
            });
        }
        return catalog;
    }
}
