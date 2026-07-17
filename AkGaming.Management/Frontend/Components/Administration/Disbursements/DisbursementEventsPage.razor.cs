using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.Disbursements;

public partial class DisbursementEventsPage : ComponentBase
{
    [Inject] private DisbursementsApiClient Api { get; set; } = null!;
    private List<DisbursementEventDto>? _events;
    private SaveDisbursementEventRequest _newEvent = new();
    private bool _showCreate;
    private bool _busy;
    private string? _error;
    protected override async Task OnInitializedAsync() => await LoadAsync();
    private async Task LoadAsync() { var result = await Api.GetEventsAsync(); if (result.IsSuccess) _events = result.Value?.ToList() ?? []; else { _events = []; _error = result.Error; } }
    private async Task CreateAsync()
    {
        _busy = true; _error = null; var result = await Api.CreateEventAsync(_newEvent); _busy = false;
        if (!result.IsSuccess) { _error = result.Error; return; }
        _newEvent = new(); _showCreate = false; await LoadAsync();
    }
}
