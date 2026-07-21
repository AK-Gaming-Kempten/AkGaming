using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.BoardManagement.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Management.Frontend.Components.BoardManagement;

public partial class BoardMeetingsPage : ComponentBase
{
    [Inject] private BoardMeetingsApiClient Api { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = null!;

    private List<BoardMeetingSummaryDto>? _meetings;
    private List<BoardAgendaItemDto>? _backlog;
    private BoardAgendaItemDto? _deletingBacklogItem;
    private Guid? _draggedBacklogItemId;
    private bool _canManage;
    private bool _showCreate;
    private bool _showBacklogCreate;
    private bool _busy;
    private string _backlogTitle = string.Empty;
    private string? _backlogDescription;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var authorizationResult = await AuthorizationService.AuthorizeAsync(
            authenticationState.User,
            "management.board-meetings.manage");

        _canManage = authorizationResult.Succeeded;
        await LoadAsync();
    }

    private void OpenCreateDialog()
    {
        _error = null;
        _showCreate = true;
    }
    private void CloseCreateDialog()
    {
        if (!_busy)
        {
            _showCreate = false;
        }
    }

    private void ToggleBacklogCreate()
    {
        _showBacklogCreate = !_showBacklogCreate;
    }

    private async Task LoadAsync()
    {
        var meetings = await Api.GetMeetingsAsync();
        var backlog = await Api.GetBacklogAsync();

        _meetings = meetings.Value?.ToList() ?? [];
        _backlog = backlog.Value?.ToList() ?? [];
        _error = !meetings.IsSuccess ? meetings.Error : !backlog.IsSuccess ? backlog.Error : null;
    }

    private async Task CreateAsync(CreateBoardMeetingRequest request)
    {
        _busy = true;
        _error = null;

        var result = await Api.CreateMeetingAsync(request);
        _busy = false;

        if (!result.IsSuccess)
        {
            _error = result.Error;
            return;
        }

        Navigation.NavigateTo($"board/meetings/{result.Value!.Id}");
    }

    private async Task CreateBacklogItemAsync()
    {
        _busy = true;
        _error = null;

        var request = new SaveBoardAgendaItemRequest(
            _backlogTitle,
            _backlogDescription,
            null,
            _backlog?.Count ?? 0);
        var result = await Api.CreateAgendaItemAsync(request);
        _busy = false;

        if (!result.IsSuccess)
        {
            _error = result.Error;
            return;
        }

        _backlogTitle = string.Empty;
        _backlogDescription = null;
        _showBacklogCreate = false;
        await LoadAsync();
    }

    private void StartBacklogDrag(Guid itemId)
    {
        _draggedBacklogItemId = itemId;
    }

    private void EndBacklogDrag()
    {
        _draggedBacklogItemId = null;
    }

    private void OpenDeleteBacklogDialog(Guid itemId)
    {
        _deletingBacklogItem = _backlog?.SingleOrDefault(x => x.Id == itemId);
        _error = null;
    }

    private void CloseDeleteBacklogDialog()
    {
        if (!_busy)
        {
            _deletingBacklogItem = null;
        }
    }

    private async Task DeleteBacklogItemAsync()
    {
        if (_deletingBacklogItem is null) return;
        _busy = true;
        _error = null;
        var result = await Api.DeleteAgendaItemAsync(_deletingBacklogItem.Id);
        _busy = false;
        _error = result.IsSuccess ? null : result.Error;
        if (!result.IsSuccess) return;
        _deletingBacklogItem = null;
        await LoadAsync();
    }

    private async Task AssignDraggedBacklogItemAsync(Guid meetingId)
    {
        if (!_canManage || _draggedBacklogItemId is not { } itemId)
        {
            return;
        }

        _busy = true;
        _error = null;

        var result = await Api.AssignAgendaItemsAsync(meetingId, [itemId]);

        _busy = false;
        _draggedBacklogItemId = null;

        if (!result.IsSuccess)
        {
            _error = result.Error;
            return;
        }

        await LoadAsync();
    }
}
