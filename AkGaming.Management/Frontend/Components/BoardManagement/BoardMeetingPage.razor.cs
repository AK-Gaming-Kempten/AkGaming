using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.BoardManagement.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Management.Frontend.Components.BoardManagement;

public partial class BoardMeetingPage : ComponentBase
{
    [Parameter] public Guid MeetingId { get; set; }
    [Inject] private BoardMeetingsApiClient Api { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationState { get; set; } = null!;
    [Inject] private IAuthorizationService Authorization { get; set; } = null!;

    private BoardMeetingDto? _meeting;
    private IReadOnlyList<BoardAgendaItemDto> _backlogItems = [];
    private bool _busy;
    private bool _canManage;
    private bool _showProposal;
    private bool _showAgendaDialog;
    private bool _showBacklogDialog;
    private DateTime _newDateLocal;
    private int _newDuration;
    private string? _reason;
    private string _agendaTitle = string.Empty;
    private string? _agendaDescription;
    private Guid? _editingAgendaItemId;
    private Guid? _draggedAgendaItemId;
    private BoardAgendaItemDto? _deletingAgendaItem;
    private string? _error;

    protected override async Task OnParametersSetAsync()
    {
        var user = (await AuthenticationState.GetAuthenticationStateAsync()).User;
        _canManage = (await Authorization.AuthorizeAsync(user, "management.board-meetings.manage")).Succeeded;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var result = await Api.GetMeetingAsync(MeetingId);
        _meeting = result.Value;
        _error = result.IsSuccess ? null : result.Error;
        if (_meeting is not null && _newDateLocal == default)
        {
            _newDateLocal = _meeting.ScheduledAtUtc.ToLocalTime().DateTime;
            _newDuration = _meeting.DurationMinutes;
        }
    }

    private void ToggleProposal() => _showProposal = !_showProposal;

    private async Task SetAvailabilityAsync(BoardAvailabilityStatusDto status)
    {
        _busy = true;
        var result = await Api.SetAvailabilityAsync(MeetingId, status);
        _busy = false;
        _error = result.IsSuccess ? null : result.Error;
        if (result.IsSuccess) await LoadAsync();
    }

    private async Task ProposeAsync()
    {
        _busy = true;
        var result = await Api.ProposeAsync(MeetingId, new CreateRescheduleProposalRequest(ToUtc(_newDateLocal), _newDuration, _reason));
        _busy = false;
        _error = result.IsSuccess ? null : result.Error;
        if (result.IsSuccess)
        {
            _showProposal = false;
            await LoadAsync();
        }
    }

    private async Task RescheduleImmediatelyAsync()
    {
        _busy = true;
        var result = await Api.RescheduleAsync(MeetingId, new RescheduleBoardMeetingRequest(ToUtc(_newDateLocal), _newDuration, _reason));
        _busy = false;
        _error = result.IsSuccess ? null : result.Error;
        if (result.IsSuccess)
        {
            _showProposal = false;
            await LoadAsync();
        }
    }

    private async Task DecideAsync(Guid proposalId, bool accept)
    {
        _busy = true;
        var result = await Api.DecideProposalAsync(MeetingId, proposalId, accept);
        _busy = false;
        _error = result.IsSuccess ? null : result.Error;
        if (result.IsSuccess) await LoadAsync();
    }

    private async Task CancelMeetingAsync()
    {
        _busy = true;
        var result = await Api.CancelAsync(MeetingId);
        _busy = false;
        _error = result.IsSuccess ? null : result.Error;
        if (result.IsSuccess) await LoadAsync();
    }

    private void OpenAgendaCreateDialog()
    {
        _editingAgendaItemId = null;
        _agendaTitle = string.Empty;
        _agendaDescription = null;
        _error = null;
        _showAgendaDialog = true;
    }

    private void StartAgendaEdit(Guid itemId)
    {
        var item = _meeting?.AgendaItems.SingleOrDefault(x => x.Id == itemId);
        if (item is null) return;
        _editingAgendaItemId = item.Id;
        _agendaTitle = item.Title;
        _agendaDescription = item.Description;
        _error = null;
        _showAgendaDialog = true;
    }

    private void CloseAgendaDialog()
    {
        if (_busy) return;
        _showAgendaDialog = false;
        _editingAgendaItemId = null;
    }

    private async Task SaveAgendaAsync()
    {
        if (_meeting is null) return;
        _busy = true;
        if (_editingAgendaItemId.HasValue)
        {
            var item = _meeting.AgendaItems.SingleOrDefault(x => x.Id == _editingAgendaItemId.Value);
            if (item is null)
            {
                _busy = false;
                return;
            }
            var updateResult = await Api.UpdateAgendaItemAsync(item.Id, new SaveBoardAgendaItemRequest(_agendaTitle, _agendaDescription, MeetingId, item.Order));
            _error = updateResult.IsSuccess ? null : updateResult.Error;
            _busy = false;
            if (!updateResult.IsSuccess) return;
        }
        else
        {
            var createResult = await Api.CreateAgendaItemAsync(new SaveBoardAgendaItemRequest(_agendaTitle, _agendaDescription, MeetingId, _meeting.AgendaItems.Count));
            _error = createResult.IsSuccess ? null : createResult.Error;
            _busy = false;
            if (!createResult.IsSuccess) return;
        }
        _showAgendaDialog = false;
        _editingAgendaItemId = null;
        await LoadAsync();
    }

    private async Task CompleteAgendaAsync(Guid itemId)
    {
        _busy = true;
        var result = await Api.ChangeAgendaStatusAsync(itemId, BoardAgendaItemStatusDto.Completed);
        _busy = false;
        _error = result.IsSuccess ? null : result.Error;
        if (result.IsSuccess) await LoadAsync();
    }

    private async Task PostponeAgendaAsync(Guid itemId)
    {
        _busy = true;
        var result = await Api.MoveAgendaItemAsync(itemId, null, 0);
        _busy = false;
        _error = result.IsSuccess ? null : result.Error;
        if (result.IsSuccess) await LoadAsync();
    }

    private void OpenDeleteAgendaDialog(Guid itemId)
    {
        _deletingAgendaItem = _meeting?.AgendaItems.SingleOrDefault(x => x.Id == itemId);
        _error = null;
    }

    private void CloseDeleteAgendaDialog()
    {
        if (!_busy)
        {
            _deletingAgendaItem = null;
        }
    }

    private async Task DeleteAgendaAsync()
    {
        if (_deletingAgendaItem is null) return;
        _busy = true;
        _error = null;
        var result = await Api.DeleteAgendaItemAsync(_deletingAgendaItem.Id);
        _busy = false;
        _error = result.IsSuccess ? null : result.Error;
        if (!result.IsSuccess) return;
        _deletingAgendaItem = null;
        await LoadAsync();
    }

    private void StartAgendaDrag(Guid itemId)
    {
        _draggedAgendaItemId = itemId;
    }

    private void EndAgendaDrag()
    {
        _draggedAgendaItemId = null;
    }

    private async Task DropAgendaItemAtPositionAsync(int position)
    {
        if (_meeting is null || !_draggedAgendaItemId.HasValue) return;
        var orderedItems = _meeting.AgendaItems.OrderBy(x => x.Order).ToList();
        var sourceIndex = orderedItems.FindIndex(x => x.Id == _draggedAgendaItemId.Value);
        if (sourceIndex < 0) return;
        var draggedItem = orderedItems.Single(x => x.Id == _draggedAgendaItemId.Value);
        orderedItems.Remove(draggedItem);
        var insertionIndex = sourceIndex < position ? position - 1 : position;
        insertionIndex = Math.Clamp(insertionIndex, 0, orderedItems.Count);
        orderedItems.Insert(insertionIndex, draggedItem);
        _meeting = _meeting with { AgendaItems = orderedItems };
        _draggedAgendaItemId = null;
        _busy = true;
        var result = await Api.ReorderAgendaItemsAsync(MeetingId, orderedItems.Select(x => x.Id).ToList());
        _busy = false;
        _error = result.IsSuccess ? null : result.Error;
        if (result.IsSuccess) _meeting = _meeting with { AgendaItems = result.Value! };
        else await LoadAsync();
    }

    private async Task OpenBacklogDialogAsync()
    {
        _error = null;
        var result = await Api.GetBacklogAsync();
        _backlogItems = result.Value ?? [];
        _error = result.IsSuccess ? null : result.Error;
        _showBacklogDialog = true;
    }

    private void CloseBacklogDialog()
    {
        if (!_busy) _showBacklogDialog = false;
    }

    private async Task AssignBacklogItemsAsync(IReadOnlyList<Guid> itemIds)
    {
        _busy = true;
        var result = await Api.AssignAgendaItemsAsync(MeetingId, itemIds);
        _busy = false;
        _error = result.IsSuccess ? null : result.Error;
        if (!result.IsSuccess) return;
        _showBacklogDialog = false;
        _meeting = result.Value;
    }

    private static DateTimeOffset ToUtc(DateTime local)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Local)).ToUniversalTime();
    }
}
