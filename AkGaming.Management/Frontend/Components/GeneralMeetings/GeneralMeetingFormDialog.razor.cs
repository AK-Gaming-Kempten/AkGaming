using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.GeneralMeetings;

public partial class GeneralMeetingFormDialog : ComponentBase
{
    [Parameter] public string Eyebrow { get; set; } = "General meeting";
    [Parameter, EditorRequired] public required string Title { get; set; }
    [Parameter] public string? Description { get; set; }
    [Parameter, EditorRequired] public required RenderFragment ChildContent { get; set; }
    [Parameter, EditorRequired] public required RenderFragment Footer { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
}
