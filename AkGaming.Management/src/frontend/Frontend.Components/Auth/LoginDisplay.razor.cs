using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Frontend.Components.Auth;

public partial class LoginDisplay : ComponentBase {
    
    private ClaimsPrincipal? user;

    protected override async Task OnInitializedAsync()
    {
        user = (await AuthStateProvider.GetAuthenticationStateAsync()).User;
    }
    private async Task Login()
    {
        Navigation.NavigateTo("/authentication/login", true);
    }

    private async Task Logout()
    {
        Navigation.NavigateTo("/authentication/logout", true);
    }
}
