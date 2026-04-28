using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class MarkdownContent : ComponentBase
{
    [Parameter] public string? Markdown { get; set; }

    private string RenderedHtml => MarkdownRenderer.Render(Markdown);
}
