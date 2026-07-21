using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AkGaming.Management.Frontend.Components.BoardManagement;

public partial class BoardAgendaDropZone : ComponentBase
{
    [Parameter] public int Position { get; set; }
    [Parameter] public bool IsEnabled { get; set; }
    [Parameter] public EventCallback<int> OnDropped { get; set; }

    private bool _isDragOver;

    private void HandleDragEnter(DragEventArgs _)
    {
        if (IsEnabled)
        {
            _isDragOver = true;
        }
    }

    private void HandleDragLeave(DragEventArgs _)
    {
        _isDragOver = false;
    }

    private async Task HandleDropAsync(DragEventArgs _)
    {
        if (!IsEnabled)
        {
            return;
        }

        _isDragOver = false;
        await OnDropped.InvokeAsync(Position);
    }
}
