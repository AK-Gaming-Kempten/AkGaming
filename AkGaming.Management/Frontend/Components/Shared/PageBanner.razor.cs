using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Shared;

public partial class PageBanner : ComponentBase
{
    [Parameter, EditorRequired] public string Icon { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string Description { get; set; } = string.Empty;
    [Parameter] public RenderFragment? Actions { get; set; }

    private string HeadingId { get; } = $"page-banner-{Guid.NewGuid():N}";
}
