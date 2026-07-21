using AkGaming.Management.Modules.BoardManagement.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AkGaming.Management.Frontend.Components.BoardManagement;

public partial class BoardMeetingSummaryCard : ComponentBase
{
    [Parameter, EditorRequired] public required BoardMeetingSummaryDto Meeting { get; set; }
    [Parameter] public bool CanAcceptAgendaDrop { get; set; }
    [Parameter] public bool IsAgendaDragActive { get; set; }
    [Parameter] public EventCallback<Guid> OnAgendaItemDropped { get; set; }

    private bool _isDragOver;

    private string StatusCssClass
    {
        get
        {
            return Meeting.Status == BoardMeetingStatusDto.Cancelled ? "cancelled" : "scheduled";
        }
    }

    private void HandleDragEnter(DragEventArgs _)
    {
        if (CanAcceptAgendaDrop && IsAgendaDragActive) _isDragOver = true;
    }

    private void HandleDragLeave(DragEventArgs _)
    {
        _isDragOver = false;
    }

    private async Task HandleDropAsync(DragEventArgs _)
    {
        if (!CanAcceptAgendaDrop) return;
        _isDragOver = false;
        await OnAgendaItemDropped.InvokeAsync(Meeting.Id);
    }
}
