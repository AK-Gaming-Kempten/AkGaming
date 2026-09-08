using System.Security.Claims;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared;
public partial class MainLayout
{
    [Inject] private IUserSession Session { get; set; } = default!;
    private ClaimsPrincipal _user = new();
    private bool _signedIn;
    protected override async Task OnInitializedAsync() { _user = await Session.UserAsync(); _signedIn = _user.Identity?.IsAuthenticated == true; }
    private bool Has(string permission) => _user.HasClaim("permission", permission);
    private Task ToggleLogin() => _signedIn ? Session.LogoutAsync() : Session.LoginAsync();
}

