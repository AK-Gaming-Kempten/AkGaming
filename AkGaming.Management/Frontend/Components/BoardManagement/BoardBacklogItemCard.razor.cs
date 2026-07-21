using AkGaming.Management.Modules.BoardManagement.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AkGaming.Management.Frontend.Components.BoardManagement;

public partial class BoardBacklogItemCard : ComponentBase
{
    [Parameter, EditorRequired] public required BoardAgendaItemDto Item { get; set; }
    [Parameter] public bool CanManage { get; set; }
    [Parameter] public bool IsDragging { get; set; }
    [Parameter] public EventCallback<Guid> OnDragStarted { get; set; }
    [Parameter] public EventCallback OnDragEnded { get; set; }
    [Parameter] public EventCallback<Guid> OnDelete { get; set; }

    private Task HandleDragStarted(DragEventArgs _) => OnDragStarted.InvokeAsync(Item.Id);
    private Task HandleDragEnded(DragEventArgs _) => OnDragEnded.InvokeAsync();
    private Task DeleteAsync() => OnDelete.InvokeAsync(Item.Id);
}
