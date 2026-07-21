using AkGaming.Management.Modules.BoardManagement.Contracts;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.BoardManagement;

public partial class BoardAgendaDeleteDialog : ComponentBase
{
    [Parameter, EditorRequired] public required BoardAgendaItemDto Item { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public string? Error { get; set; }
    [Parameter] public EventCallback OnConfirm { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
}
