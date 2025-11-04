using Microsoft.AspNetCore.Components;

namespace Frontend.Blazor.Components.Membership;

public partial class NoMemberPanel : ComponentBase {
    private enum PanelMode
    {
        None,
        RequestLink,
        Apply
    }

    private PanelMode _mode = PanelMode.None;
    
    [Parameter] public Guid UserId { get; set; }

    [Inject] NavigationManager Nav { get; set; } = default!;
    
    private void ShowRequestLink() => _mode = PanelMode.RequestLink;
    private void ShowApply()       => _mode = PanelMode.Apply;
}