using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.GeneralMeetings.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AkGaming.Management.Frontend.Components.GeneralMeetings;

public partial class BallotPanel : ComponentBase
{
    [Inject] private GeneralMeetingsApiClient Api { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;
    [Parameter, EditorRequired] public BallotDto Ballot { get; set; } = null!;
    [Parameter] public Guid MeetingId { get; set; }
    [Parameter] public bool CanManage { get; set; }
    [Parameter] public IReadOnlyList<AttendanceDto> Attendees { get; set; } = [];
    [Parameter] public EventCallback OnChanged { get; set; }
    [Parameter] public EventCallback<BallotDto> OnEdit { get; set; }
    private string? _credential;
    private string? _offlineCredential;
    private string? _error;
    private Guid? _offlineMember;
    private readonly HashSet<Guid> _selected = [];
    private bool _submitted;
    private bool _showVotingDialog;
    private bool UsesSingleChoice => Ballot.Type == BallotTypeDto.YesNoAbstain || Ballot.MaximumSelections == 1;
    private bool IsLeadingResult(BallotResultDto result) { var highestVoteCount = Ballot.Results?.Max(x => x.Votes) ?? 0; return highestVoteCount > 0 && result.Votes == highestVoteCount; }
    private async Task OpenAsync() { if (!await Confirm("Open this ballot? Eligibility will be frozen from the currently checked-in members.")) return; var result = await Api.OpenBallotAsync(MeetingId, Ballot.Id); await Finish(result.IsSuccess, result.Error); }
    private async Task CloseAsync() { if (!await Confirm("Close this ballot? No further votes will be accepted.")) return; var result = await Api.CloseBallotAsync(MeetingId, Ballot.Id); await Finish(result.IsSuccess, result.Error); }
    private void OpenVotingDialog() { _error = null; _showVotingDialog = true; }
    private void CloseVotingDialog() { _showVotingDialog = false; }
    private async Task IssueOwnAsync() { var result = await Api.IssueOwnCredentialAsync(Ballot.Id); if (result.IsSuccess) _credential = result.Value!.Credential; else _error = result.Error; }
    private async Task IssueOfflineAsync() { if (!_offlineMember.HasValue) return; var result = await Api.IssueCredentialAsync(Ballot.Id, _offlineMember.Value); if (result.IsSuccess) _offlineCredential = result.Value!.Credential; else _error = result.Error; }
    private void SelectSingle(Guid optionId) { _selected.Clear(); _selected.Add(optionId); }
    private void Toggle(Guid optionId, bool selected) { if (selected) { if (_selected.Count < Ballot.MaximumSelections) _selected.Add(optionId); } else _selected.Remove(optionId); }
    private async Task VoteAsync() { if (string.IsNullOrWhiteSpace(_credential)) return; if (!await Confirm("Submit this secret vote? It cannot be changed afterwards.")) return; var result = await Api.CastVoteAsync(Ballot.Id, new CastVoteRequest(_credential, _selected.ToList())); if (result.IsSuccess) { _submitted = true; _credential = null; await OnChanged.InvokeAsync(); } else _error = result.Error; }
    private async Task Finish(bool success, string? error) { _error = success ? null : error; if (success) await OnChanged.InvokeAsync(); }
    private ValueTask<bool> Confirm(string message) => Js.InvokeAsync<bool>("confirm", message);
}
