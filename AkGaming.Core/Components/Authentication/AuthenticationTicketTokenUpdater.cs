using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AkGaming.Core.Components.Authentication;

public sealed class AuthenticationTicketTokenUpdater(
    IHttpContextAccessor httpContextAccessor,
    IOptionsMonitor<CookieAuthenticationOptions> cookieOptionsMonitor,
    ILogger<AuthenticationTicketTokenUpdater> logger)
{
    private const string SessionIdClaim = "Microsoft.AspNetCore.Authentication.Cookies-SessionId";

    public async Task UpdateTokensAsync(
        string authenticationScheme,
        string accessToken,
        string refreshToken,
        string expiresAt,
        CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
            return;

        var cookieOptions = cookieOptionsMonitor.Get(authenticationScheme);
        if (cookieOptions.SessionStore is null)
            return;

        var sessionKey = TryGetSessionKey(httpContext, cookieOptions);
        if (string.IsNullOrWhiteSpace(sessionKey))
            return;

        var ticket = await cookieOptions.SessionStore.RetrieveAsync(sessionKey);
        if (ticket is null)
            return;

        StoreToken(ticket.Properties, "access_token", accessToken);
        StoreToken(ticket.Properties, "refresh_token", refreshToken);
        StoreToken(ticket.Properties, "expires_at", expiresAt);

        try
        {
            await cookieOptions.SessionStore.RenewAsync(sessionKey, ticket);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Failed to persist refreshed OIDC tokens to the authentication ticket.");
        }
    }

    private static string? TryGetSessionKey(HttpContext httpContext, CookieAuthenticationOptions cookieOptions)
    {
        var cookieName = cookieOptions.Cookie.Name;
        if (string.IsNullOrWhiteSpace(cookieName))
            return null;

        var cookie = cookieOptions.CookieManager.GetRequestCookie(httpContext, cookieName);
        if (string.IsNullOrWhiteSpace(cookie))
            return null;

        var ticket = cookieOptions.TicketDataFormat.Unprotect(cookie, GetTlsTokenBinding(httpContext));
        return ticket?.Principal.FindFirst(SessionIdClaim)?.Value;
    }

    private static string? GetTlsTokenBinding(HttpContext httpContext)
    {
        var binding = httpContext.Features.Get<ITlsTokenBindingFeature>()?.GetProvidedTokenBindingId();
        return binding is null ? null : Convert.ToBase64String(binding);
    }

    private static void StoreToken(AuthenticationProperties properties, string name, string value)
    {
        var tokens = properties.GetTokens().ToList();
        var token = tokens.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.Ordinal));

        if (token is null)
        {
            tokens.Add(new AuthenticationToken { Name = name, Value = value });
        }
        else
        {
            token.Value = value;
        }

        properties.StoreTokens(tokens);
    }
}
