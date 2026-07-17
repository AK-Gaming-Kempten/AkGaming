using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AkGaming.Core.Components.Navigation;

public partial class ContextMenu : ComponentBase
{
    [Parameter] public string AriaLabel { get; set; } = "More actions";
    [Parameter] public bool OpenUpward { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    internal bool IsOpen { get; private set; }

    private void Toggle()
    {
        IsOpen = !IsOpen;
    }

    private void HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            IsOpen = false;
        }
    }

    internal void Close()
    {
        IsOpen = false;
    }
}
