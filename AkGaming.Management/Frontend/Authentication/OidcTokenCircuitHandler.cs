using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace AkGaming.Management.Frontend.Authentication;

internal sealed class OidcTokenCircuitHandler(
    IHttpContextAccessor httpContextAccessor,
    OidcTokenStore tokenStore)
    : CircuitHandler {
    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken) {
        return InitializeAsync();
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken) {
        return InitializeAsync();
    }

    private async Task InitializeAsync() {
        if (tokenStore.IsInitialized)
            return;

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
            return;

        var accessToken = await httpContext.GetTokenAsync("access_token");
        var refreshToken = await httpContext.GetTokenAsync("refresh_token");
        var expiresAt = await httpContext.GetTokenAsync("expires_at");
        tokenStore.Initialize(accessToken, refreshToken, expiresAt);
    }
}
