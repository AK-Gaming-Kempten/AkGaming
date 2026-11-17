using Microsoft.AspNetCore.Components;

namespace AkGaming.Gamenight.Shared.Components;

public partial class FormDialog : ComponentBase
{
    [Parameter] public bool Compact { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter] public string Eyebrow { get; set; } = "Game Night";
    [Parameter, EditorRequired] public required string Title { get; set; }
    [Parameter] public string? Description { get; set; }
    [Parameter, EditorRequired] public required RenderFragment ChildContent { get; set; }
    [Parameter, EditorRequired] public required RenderFragment Footer { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
}
