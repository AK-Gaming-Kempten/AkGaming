using Microsoft.AspNetCore.Components;

namespace AkGaming.Core.Components.Layout;

public partial class BlazorErrorDisplay : ComponentBase
{
    [Parameter] public string AppName { get; set; } = "AK Gaming";
    [Parameter] public string Title { get; set; } = "Unexpected client error";
    [Parameter] public string Message { get; set; } = "The current page hit an unexpected error and may no longer respond correctly.";
    [Parameter] public string SubtleMessage { get; set; } = "Reload the page to restore a clean session. If the issue keeps happening, check the browser console and server logs.";
    [Parameter] public string ReloadText { get; set; } = "Reload page";
}
