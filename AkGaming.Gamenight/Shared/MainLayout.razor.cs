using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
namespace AkGaming.Gamenight.Shared;
public partial class MainLayout
{
    [Inject] private IUserSession Session { get; set; } = default!;
    private ClaimsPrincipal _user = new();
    private bool _signedIn, _menuOpen;
    protected override async Task OnInitializedAsync() { _user = await Session.UserAsync(); _signedIn = _user.Identity?.IsAuthenticated == true; }
    private bool Has(string permission) => _user.HasClaim("permission", permission);
    private Task ToggleLogin() => _signedIn ? Session.LogoutAsync() : Session.LoginAsync();
    private void ToggleMenu() => _menuOpen = !_menuOpen;
    private void CloseMenu() => _menuOpen = false;
    private void HandleHeaderKeyDown(KeyboardEventArgs e) { if (e.Key == "Escape") CloseMenu(); }
}
