using Microsoft.AspNetCore.Components;

namespace Frontend.Blazor.Components.Account;

public partial class AccessDenied : ComponentBase
{
    private void Logout() {
        Navigation.NavigateTo("/logout", forceLoad: true);
    }

    [Inject] NavigationManager Navigation { get; set; } = default!;
}