using Microsoft.AspNetCore.Components;

namespace AkGaming.Core.Components.Navigation;

public partial class ContextMenuItem : ComponentBase
{
    [CascadingParameter] private ContextMenu? ContextMenu { get; set; }

    [Parameter] public string Text { get; set; } = string.Empty;
    [Parameter] public string IconClass { get; set; } = "bi-circle";
    [Parameter] public string? Href { get; set; }
    [Parameter] public bool OpenInNewTab { get; set; }
    [Parameter] public bool IsDestructive { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }

    private async Task SelectAsync()
    {
        await OnClick.InvokeAsync();
        ContextMenu?.Close();
    }
}
