using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components;

public partial class Home : ComponentBase {
    [Inject]
    private NavigationManager Nav { get; set; } = null!;
    
    private void NavigateToMembership() => Nav.NavigateTo("/membership");
    private void NavigateToMemberManagement() => Nav.NavigateTo("/member-management");
}