using AkGaming.Management.Modules.BoardManagement.Contracts;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.BoardManagement;

public partial class BoardMeetingCreateDialog : ComponentBase
{
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public string? Error { get; set; }
    [Parameter] public IReadOnlyList<BoardAgendaItemDto> BacklogItems { get; set; } = [];
    [Parameter] public EventCallback<CreateBoardMeetingRequest> OnSubmit { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private readonly List<DraftAgendaItem> _agendaItems = [];
    private Guid? _draggedAgendaItemId;
    private bool _showBacklogSelection;
    private string _title = "Board meeting";
    private DateTime _scheduledLocal = DateTime.Now.AddDays(7);
    private int _durationMinutes = 90;
    private string? _location;

    private void AddAgendaItem()
    {
        _agendaItems.Add(new DraftAgendaItem());
    }

    private void RemoveAgendaItem(DraftAgendaItem item)
    {
        _agendaItems.Remove(item);
    }

    private IReadOnlyList<BoardAgendaItemDto> AvailableBacklogItems => BacklogItems
        .Where(item => _agendaItems.All(draft => draft.BacklogItemId != item.Id))
        .OrderBy(item => item.Order)
        .ThenBy(item => item.Title)
        .ToList();

    private void OpenBacklogSelection()
    {
        _showBacklogSelection = true;
    }

    private void CloseBacklogSelection()
    {
        _showBacklogSelection = false;
    }

    private void AddBacklogItems(IReadOnlyList<Guid> itemIds)
    {
        var itemsById = AvailableBacklogItems.ToDictionary(item => item.Id);
        foreach (var itemId in itemIds)
        {
            if (!itemsById.TryGetValue(itemId, out var item)) continue;
            _agendaItems.Add(new DraftAgendaItem
            {
                Title = item.Title,
                Description = item.Description,
                BacklogItemId = item.Id
            });
        }
        _showBacklogSelection = false;
    }

    private void StartDragging(Guid itemId)
    {
        _draggedAgendaItemId = itemId;
    }

    private void StopDragging()
    {
        _draggedAgendaItemId = null;
    }

    private void DropAtPosition(int position)
    {
        if (!_draggedAgendaItemId.HasValue) return;
        var sourceIndex = _agendaItems.FindIndex(x => x.Id == _draggedAgendaItemId.Value);
        if (sourceIndex < 0) return;
        var draggedItem = _agendaItems.Single(x => x.Id == _draggedAgendaItemId.Value);
        _agendaItems.Remove(draggedItem);
        var insertionIndex = sourceIndex < position ? position - 1 : position;
        insertionIndex = Math.Clamp(insertionIndex, 0, _agendaItems.Count);
        _agendaItems.Insert(insertionIndex, draggedItem);
        _draggedAgendaItemId = null;
    }

    private async Task SubmitAsync()
    {
        var agendaItems = _agendaItems
            .Select(x => new CreateBoardAgendaItemRequest(x.Title, x.Description, x.BacklogItemId))
            .ToList();
        var request = new CreateBoardMeetingRequest(
            _title,
            ToUtc(_scheduledLocal),
            _durationMinutes,
            _location,
            agendaItems);
        await OnSubmit.InvokeAsync(request);
    }

    private static DateTimeOffset ToUtc(DateTime local)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Local)).ToUniversalTime();
    }

    private sealed class DraftAgendaItem
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? BacklogItemId { get; set; }
    }
}
