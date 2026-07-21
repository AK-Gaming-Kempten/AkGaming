using AkGaming.Management.Modules.BoardManagement.Contracts;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.BoardManagement;

public partial class BoardBacklogSelectionDialog : ComponentBase
{
    [Parameter, EditorRequired] public required IReadOnlyList<BoardAgendaItemDto> Items { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public string? Error { get; set; }
    [Parameter] public EventCallback<IReadOnlyList<Guid>> OnSubmit { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private readonly HashSet<Guid> _selectedIds = [];

    private bool IsSelected(Guid itemId) => _selectedIds.Contains(itemId);

    private void Toggle(Guid itemId, ChangeEventArgs args)
    {
        if (args.Value is true) _selectedIds.Add(itemId);
        else _selectedIds.Remove(itemId);
    }

    private async Task SubmitAsync()
    {
        var orderedIds = Items.Where(x => _selectedIds.Contains(x.Id)).Select(x => x.Id).ToList();
        await OnSubmit.InvokeAsync(orderedIds);
    }
}
