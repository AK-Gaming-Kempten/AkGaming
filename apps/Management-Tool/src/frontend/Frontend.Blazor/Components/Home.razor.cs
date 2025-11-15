using Microsoft.AspNetCore.Components;

namespace Frontend.Blazor.Components;

public partial class Home : ComponentBase {
    [Inject]
    private NavigationManager Nav { get; set; } = null!;
    
    private void NavigateToMembership() => Nav.NavigateTo("/membership");
    private void NavigateToMemberManagement() => Nav.NavigateTo("/member-management");
}