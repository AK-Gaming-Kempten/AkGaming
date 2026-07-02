using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace AkGaming.Identity.Api.OpenIddict;

internal sealed class OpenCloudRedirectUriHandler :
    IOpenIddictServerHandler<OpenIddictServerEvents.ValidateAuthorizationRequestContext>
{
    private const string OpenCloudClientIdPrefix = "OpenCloud";

    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly ILogger<OpenCloudRedirectUriHandler> _logger;

    public OpenCloudRedirectUriHandler(
        IOpenIddictApplicationManager applicationManager,
        ILogger<OpenCloudRedirectUriHandler> logger)
    {
        _applicationManager = applicationManager;
        _logger = logger;
    }

    public async ValueTask HandleAsync(OpenIddictServerEvents.ValidateAuthorizationRequestContext context)
    {
        if (context.Request is null
            || string.IsNullOrWhiteSpace(context.ClientId)
            || string.IsNullOrWhiteSpace(context.Request.RedirectUri))
        {
            return;
        }

        var application = await _applicationManager.FindByClientIdAsync(context.ClientId, context.CancellationToken);
        if (application is null)
        {
            return;
        }

        var registeredRedirectUris = await _applicationManager.GetRedirectUrisAsync(application, context.CancellationToken);
        if (registeredRedirectUris.Any(uri => string.Equals(uri, context.Request.RedirectUri, StringComparison.Ordinal)))
        {
            context.SetRedirectUri(context.Request.RedirectUri);
            return;
        }

        if (IsOpenCloudLoopbackRequest(context.ClientId, context.Request.RedirectUri, registeredRedirectUris))
        {
            context.SetRedirectUri(context.Request.RedirectUri);
            _logger.LogInformation(
                "Accepted OpenCloud loopback redirect URI {RedirectUri} for client {ClientId}.",
                context.Request.RedirectUri,
                context.ClientId);
            return;
        }

        context.Reject(
            error: OpenIddictConstants.Errors.InvalidRequest,
            description: "The specified 'redirect_uri' is not valid for this client application.",
            uri: null);
    }

    private static bool IsOpenCloudLoopbackRequest(
        string clientId,
        string requestedRedirectUri,
        IEnumerable<string> registeredRedirectUris)
    {
        return clientId.StartsWith(OpenCloudClientIdPrefix, StringComparison.OrdinalIgnoreCase)
               && Uri.TryCreate(requestedRedirectUri, UriKind.Absolute, out var requestedUri)
               && IsLoopbackUri(requestedUri)
               && registeredRedirectUris.Any(registeredUri => IsRegisteredLoopbackMatch(registeredUri, requestedUri));
    }

    private static bool IsRegisteredLoopbackMatch(string registeredRedirectUri, Uri requestedUri)
    {
        return Uri.TryCreate(registeredRedirectUri, UriKind.Absolute, out var registeredUri)
               && IsLoopbackUri(registeredUri)
               && string.Equals(registeredUri.Scheme, requestedUri.Scheme, StringComparison.OrdinalIgnoreCase)
               && string.Equals(registeredUri.Host, requestedUri.Host, StringComparison.OrdinalIgnoreCase)
               && string.Equals(registeredUri.AbsolutePath, requestedUri.AbsolutePath, StringComparison.Ordinal)
               && string.Equals(registeredUri.Query, requestedUri.Query, StringComparison.Ordinal);
    }

    private static bool IsLoopbackUri(Uri uri)
    {
        return uri.Scheme == Uri.UriSchemeHttp
               && (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(uri.Host, "127.0.0.1", StringComparison.Ordinal)
                   || string.Equals(uri.Host, "::1", StringComparison.Ordinal));
    }
}
