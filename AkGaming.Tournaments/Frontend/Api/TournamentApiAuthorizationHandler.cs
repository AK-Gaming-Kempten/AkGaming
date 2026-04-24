using System.Net.Http.Headers;
using AkGaming.Tournaments.Frontend.Authentication;

namespace AkGaming.Tournaments.Frontend.Api;

public sealed class TournamentApiAuthorizationHandler(OidcTokenStore tokenStore) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(tokenStore.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenStore.AccessToken);

        return base.SendAsync(request, cancellationToken);
    }
}
