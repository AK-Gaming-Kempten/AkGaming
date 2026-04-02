using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Account;

public partial class AccessDenied : ComponentBase
{
    private void Logout() {
        Navigation.NavigateTo("/authentication/logout", forceLoad: true);
    }

    [Inject] NavigationManager Navigation { get; set; } = default!;
}
