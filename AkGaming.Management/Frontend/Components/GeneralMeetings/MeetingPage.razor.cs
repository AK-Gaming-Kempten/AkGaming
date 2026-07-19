using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Frontend.Authentication;
using AkGaming.Management.Frontend.Authorization;
using AkGaming.Management.Modules.GeneralMeetings.Contracts;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using System.Net.Security;

namespace AkGaming.Management.Frontend.Components.GeneralMeetings;

public partial class MeetingPage : ComponentBase, IAsyncDisposable
{
    [Parameter] public Guid MeetingId { get; set; }
    [Inject] private GeneralMeetingsApiClient Api { get; set; } = null!;
    [Inject] private GeneralMeetingAccessService MeetingAccess { get; set; } = null!;
    [Inject] private MemberManagementApiClient MemberApi { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationState { get; set; } = null!;
    [Inject] private OidcTokenStore TokenStore { get; set; } = null!;
    [Inject] private IConfiguration Configuration { get; set; } = null!;
    [Inject] private IWebHostEnvironment Environment { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;
    private GeneralMeetingDto? _meeting;
    private List<MemberDto> _members = [];
    private HubConnection? _hub;
    private bool _canManage;
    private bool? _canAccess;
    private bool _showAgendaForm;
    private string _agendaHeading = string.Empty;
    private Guid? _editingAgendaItemId;
    private string? _agendaDescription;
    private Guid? _agendaParent;
    private int _agendaOrder;
    private Guid? _selectedMemberId;
    private Guid? _ballotAgendaItemId;
    private Guid? _editingBallotId;
    private string _ballotQuestion = string.Empty;
    private BallotTypeDto _ballotType;
    private string? _ballotOptions;
    private int _maximumSelections = 1;
    private bool _showLiveResults;
    private string? _error;
    private string? _success;
    private IEnumerable<AgendaItemDto> OrderedAgenda => _meeting is null ? [] : _meeting.AgendaItems.OrderBy(x => x.ParentId.HasValue).ThenBy(x => x.Order);
    private IEnumerable<MeetingStatusDto> _nextStatuses => _meeting?.Status switch { MeetingStatusDto.Draft => [MeetingStatusDto.CheckInOpen], MeetingStatusDto.InvitationsSent => [MeetingStatusDto.CheckInOpen], MeetingStatusDto.CheckInOpen => [MeetingStatusDto.InProgress], MeetingStatusDto.InProgress => [MeetingStatusDto.Closed], _ => [] };

    protected override async Task OnInitializedAsync()
    {
        var user = (await AuthenticationState.GetAuthenticationStateAsync()).User;
        _canAccess = await MeetingAccess.CanAccessAsync(user);
        if (!_canAccess.Value)
            return;

        _canManage = user.HasClaim("permission", "management.general-meetings.manage");
        await ReloadAsync();
        if (_canManage) { var result = await MemberApi.GetAllMembersAsync(); _members = result.Value?.ToList() ?? []; }
        await ConnectRealtimeAsync();
    }

    private async Task ReloadAsync()
    {
        var result = await Api.GetMeetingAsync(MeetingId);
        if (result.IsSuccess) { _meeting = result.Value; _error = null; }
        else _error = result.Error;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ConnectRealtimeAsync()
    {
        var baseUrl = new Uri(Configuration["Api:BaseUrl"]!);
        var hubUrl = new Uri(baseUrl, $"hubs/general-meetings?meetingId={MeetingId}");
        var allowUntrustedLocalCertificate = Environment.IsDevelopment()
                                               && Configuration.GetValue<bool>("Dev:AllowUntrustedLocalCertificates")
                                               && IsLocalHost(hubUrl.Host);
        _hub = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => {
                options.AccessTokenProvider = () => Task.FromResult(TokenStore.AccessToken);
                if (!allowUntrustedLocalCertificate)
                    return;

                options.HttpMessageHandlerFactory = handler => {
                    if (handler is HttpClientHandler httpClientHandler)
                        httpClientHandler.ServerCertificateCustomValidationCallback = static (_, _, _, _) => true;

                    return handler;
                };
                options.WebSocketConfiguration = webSocketOptions =>
                    webSocketOptions.RemoteCertificateValidationCallback = static (_, _, _, _) => true;
            })
            .WithAutomaticReconnect()
            .Build();
        foreach (var eventName in new[] { "MeetingChanged", "AgendaChanged", "MinutesChanged", "AttendanceChanged", "BallotChanged" })
            _hub.On<Guid>(eventName, ignored => { _ = InvokeAsync(ReloadAsync); });
        _hub.On<Guid, bool>("PresenceChanged", (userId, online) => { _ = InvokeAsync(ReloadAsync); });
        try { await _hub.StartAsync(); } catch (Exception exception) { _error = $"Live updates unavailable: {exception.Message}"; }
    }

    private static bool IsLocalHost(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
               || host == "127.0.0.1"
               || host == "::1";
    }

    private async Task CheckInAsync() { var result = await Api.CheckInAsync(MeetingId); await Finish(result.IsSuccess, result.Error, "Checked in."); }
    private async Task SendInvitationAsync() { if (!await Confirm("Send an invitation email to every currently eligible member?")) return; var result = await Api.DispatchInvitationsAsync(MeetingId, false, null); await Finish(result.IsSuccess, result.Error, "Invitations dispatched."); }
    private async Task SendReminderAsync() { if (!await Confirm("Send a meeting reminder to every currently eligible member?")) return; var result = await Api.DispatchInvitationsAsync(MeetingId, true, null); await Finish(result.IsSuccess, result.Error, "Reminders dispatched."); }
    private async Task ChangeStatusAsync(MeetingStatusDto status) { if (!await Confirm($"Change the meeting state to {status}?")) return; var result = await Api.ChangeStatusAsync(MeetingId, status); await Finish(result.IsSuccess, result.Error, $"Meeting is now {status}."); }
    private async Task FinalizeAsync() { if (!await Confirm("Finalize this protocol? The meeting, agenda and minutes will become read-only.")) return; var result = await Api.FinalizeAsync(MeetingId); await Finish(result.IsSuccess, result.Error, "Protocol finalized."); }
    private void StartAgendaDraft() { _error = null; _editingAgendaItemId = null; _agendaHeading = string.Empty; _agendaDescription = null; _agendaParent = null; _agendaOrder = 0; _showAgendaForm = true; }
    private void CloseAgendaDialog() { _showAgendaForm = false; _editingAgendaItemId = null; }
    private async Task CreateAgendaAsync() { var request = new SaveAgendaItemRequest(_agendaParent, _agendaHeading, _agendaDescription, _agendaOrder); var result = _editingAgendaItemId.HasValue ? await Api.UpdateAgendaItemAsync(MeetingId, _editingAgendaItemId.Value, request) : await Api.CreateAgendaItemAsync(MeetingId, request); if (result.IsSuccess) CloseAgendaDialog(); await Finish(result.IsSuccess, result.Error); }
    private void EditAgenda(AgendaItemDto item) { _error = null; _editingAgendaItemId = item.Id; _agendaHeading = item.Heading; _agendaDescription = item.Description; _agendaParent = item.ParentId; _agendaOrder = item.Order; _showAgendaForm = true; }
    private async Task MakeCurrentAsync(Guid itemId) { var result = await Api.ChangeAgendaStateAsync(MeetingId, itemId, AgendaItemStatusDto.Current); await Finish(result.IsSuccess, result.Error); }
    private async Task SaveMinutesAsync(Guid itemId, string? minutes) { var result = await Api.UpdateMinutesAsync(MeetingId, itemId, minutes); await Finish(result.IsSuccess, result.Error); }
    private async Task AddAttendeeAsync() { if (!_selectedMemberId.HasValue) return; var result = await Api.SetAttendanceAsync(MeetingId, _selectedMemberId.Value, null); _selectedMemberId = null; await Finish(result.IsSuccess, result.Error); }
    private async Task ToggleAttendanceAsync(AttendanceDto attendee) { var checkedIn = !(attendee.CheckedInAt.HasValue && !attendee.CheckedOutAt.HasValue); var result = await Api.SetAttendanceAsync(MeetingId, attendee.MemberId, checkedIn); await Finish(result.IsSuccess, result.Error); }
    private void StartBallotDraft(Guid agendaItemId) { _error = null; _ballotAgendaItemId = agendaItemId; _editingBallotId = null; _ballotQuestion = string.Empty; _ballotType = BallotTypeDto.YesNoAbstain; _ballotOptions = null; _maximumSelections = 1; _showLiveResults = false; }
    private void CloseBallotDialog() { _ballotAgendaItemId = null; _editingBallotId = null; }
    private void StartBallotEdit(BallotDto ballot) { var agenda = _meeting?.AgendaItems.Single(x => x.Ballots.Any(b => b.Id == ballot.Id)); if (agenda is null) return; _error = null; _ballotAgendaItemId = agenda.Id; _editingBallotId = ballot.Id; _ballotQuestion = ballot.Question; _ballotType = ballot.Type; _ballotOptions = string.Join("; ", ballot.Options.Select(x => x.Text)); _maximumSelections = ballot.MaximumSelections; _showLiveResults = ballot.ShowResultsWhileOpen; }
    private async Task CreateBallotAsync() { if (!_ballotAgendaItemId.HasValue) return; var options = (_ballotOptions ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); var request = new SaveBallotRequest(_ballotQuestion, _ballotType, options, _maximumSelections, _showLiveResults); var result = _editingBallotId.HasValue ? await Api.UpdateBallotAsync(MeetingId, _ballotAgendaItemId.Value, _editingBallotId.Value, request) : await Api.CreateBallotAsync(MeetingId, _ballotAgendaItemId.Value, request); if (result.IsSuccess) CloseBallotDialog(); await Finish(result.IsSuccess, result.Error); }
    private int Depth(AgendaItemDto item) { var depth = 0; var parent = item.ParentId; while (parent.HasValue && depth < 4) { depth++; parent = _meeting?.AgendaItems.SingleOrDefault(x => x.Id == parent)?.ParentId; } return depth; }
    private static string MemberName(MemberDto member) { var name = $"{member.FirstName} {member.LastName}".Trim(); return string.IsNullOrWhiteSpace(name) ? member.Email ?? member.Id.ToString() : name; }
    private async Task Finish(bool success, string? error, string? message = null) { _error = success ? null : error; _success = success ? message : null; if (success) await ReloadAsync(); }
    private ValueTask<bool> Confirm(string message) => Js.InvokeAsync<bool>("confirm", message);
    public async ValueTask DisposeAsync() { if (_hub is not null) await _hub.DisposeAsync(); }
}
