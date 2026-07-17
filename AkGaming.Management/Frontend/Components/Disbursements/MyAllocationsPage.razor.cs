using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Disbursements;

public partial class MyAllocationsPage : ComponentBase
{
    [Inject] private DisbursementsApiClient Api { get; set; } = null!;
    private List<AllocationDto>? _allocations;
    private bool _busy;
    private string? _error;
    protected override async Task OnInitializedAsync() => await LoadAsync();
    private async Task LoadAsync() { _busy = true; _error = null; var result = await Api.GetMyAllocationsAsync(); _busy = false; if (result.IsSuccess) _allocations = result.Value?.ToList() ?? []; else { _allocations = []; _error = result.Error; } }
}
