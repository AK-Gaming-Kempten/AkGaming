using AkGaming.Management.Frontend.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components;

public partial class App : ComponentBase {
    [CascadingParameter] public HttpContext? HttpContext { get; set; }

    [Inject] private OidcTokenStore TokenStore { get; set; } = default!;

    protected override async Task OnInitializedAsync() {
        if (TokenStore.IsInitialized || HttpContext is null)
            return;

        var accessToken = await HttpContext.GetTokenAsync("access_token");
        var refreshToken = await HttpContext.GetTokenAsync("refresh_token");
        var expiresAt = await HttpContext.GetTokenAsync("expires_at");
        TokenStore.Initialize(accessToken, refreshToken, expiresAt);
    }
}
