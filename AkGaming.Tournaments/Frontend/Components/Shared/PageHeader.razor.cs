using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class PageHeader : ComponentBase
{
    [Parameter] public string? Eyebrow { get; set; }
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public string? Description { get; set; }
    [Parameter] public RenderFragment? Actions { get; set; }
}
