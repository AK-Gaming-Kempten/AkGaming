using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class UserBox : ComponentBase
{
    [CascadingParameter] public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    [Inject] private NavigationManager Nav { get; set; } = default!;

    private string? DisplayName;

    protected override async Task OnParametersSetAsync()
    {
        var authState = await AuthenticationStateTask;
        var user = authState.User;

        DisplayName =
            user.Claims.FirstOrDefault(claim => claim.Type == "discord_username")?.Value
            ?? user.Claims.FirstOrDefault(claim => claim.Type == "preferred_username")?.Value
            ?? user.Claims.FirstOrDefault(claim => claim.Type == "email")?.Value
            ?? user.Claims.FirstOrDefault(claim => claim.Type == "sub")?.Value
            ?? "Unknown user";
    }

    private void Login() => Nav.NavigateTo("/authentication/login", forceLoad: true);
    private void Logout() => Nav.NavigateTo("/authentication/logout", forceLoad: true);
}
