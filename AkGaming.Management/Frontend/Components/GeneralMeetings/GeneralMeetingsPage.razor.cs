using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Frontend.Authorization;
using AkGaming.Management.Modules.GeneralMeetings.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Management.Frontend.Components.GeneralMeetings;

public partial class GeneralMeetingsPage : ComponentBase
{
    [Inject] private GeneralMeetingsApiClient Api { get; set; } = null!;
    [Inject] private GeneralMeetingAccessService MeetingAccess { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationState { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    private List<GeneralMeetingSummaryDto>? _meetings;
    private bool _showCreate;
    private bool _busy;
    private string _title = string.Empty;
    private string? _location;
    private DateTime _scheduledLocal = DateTime.Now.AddDays(14);
    private string? _error;
    private bool? _canAccess;
    protected override async Task OnInitializedAsync() { var user = (await AuthenticationState.GetAuthenticationStateAsync()).User; _canAccess = await MeetingAccess.CanAccessAsync(user); if (!_canAccess.Value) return; var result = await Api.GetMeetingsAsync(); _meetings = result.Value?.ToList() ?? []; _error = result.IsSuccess ? null : result.Error; }
    private void ToggleCreate() => _showCreate = !_showCreate;
    private async Task CreateAsync()
    {
        _busy = true; _error = null;
        var request = new SaveMeetingRequest(_title, new DateTimeOffset(_scheduledLocal), _location);
        var result = await Api.CreateMeetingAsync(request); _busy = false;
        if (!result.IsSuccess) { _error = result.Error; return; }
        Navigation.NavigateTo($"general-meetings/{result.Value!.Id}");
    }
}
