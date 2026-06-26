using System.ComponentModel;
using System.Net;

namespace AkGaming.Identity.Api.IntegrationTests;

public sealed class OidcCorsTests
{
    private static readonly Uri BaseUri = new("https://localhost");
    private const string AllowedOrigin = "https://cloud.akgaming.de";
    private const string DeniedOrigin = "https://evil.example";

    [Fact]
    [Description("Verifies that OIDC clients can read the discovery document from origins derived from redirect URIs.")]
    public async Task DiscoveryDocument_WithRedirectUriOrigin_ReturnsCorsHeader()
    {
        // Arrange
        using var factory = CreateFactoryWithCloudRedirectUris();
        using var client = CreateClient(factory);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/.well-known/openid-configuration");
        request.Headers.Add("Origin", AllowedOrigin);

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Contains(AllowedOrigin, values);
    }

    [Fact]
    [Description("Verifies that origins not derived from OIDC redirect URIs cannot read the discovery document cross-origin.")]
    public async Task DiscoveryDocument_WithDeniedOrigin_DoesNotReturnCorsHeader()
    {
        // Arrange
        using var factory = CreateFactoryWithCloudRedirectUris();
        using var client = CreateClient(factory);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/.well-known/openid-configuration");
        request.Headers.Add("Origin", DeniedOrigin);

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    [Description("Verifies that OIDC clients can preflight token requests from origins derived from redirect URIs.")]
    public async Task TokenEndpointPreflight_WithRedirectUriOrigin_ReturnsCorsHeaders()
    {
        // Arrange
        using var factory = CreateFactoryWithCloudRedirectUris();
        using var client = CreateClient(factory);
        using var request = new HttpRequestMessage(HttpMethod.Options, "/connect/token");
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,authorization");

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Contains(AllowedOrigin, origins);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Methods", out var methods));
        Assert.Contains("POST", methods);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Headers", out var headers));
        var allowedHeaders = string.Join(',', headers);
        Assert.Contains("content-type", allowedHeaders, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("authorization", allowedHeaders, StringComparison.OrdinalIgnoreCase);
    }

    private static TestApiFactory CreateFactoryWithCloudRedirectUris()
    {
        return new TestApiFactory(new Dictionary<string, string?>
        {
            ["OpenIddict:Applications:0:RedirectUris:0"] = $"{AllowedOrigin}/oidc-callback.html",
            ["OpenIddict:Applications:0:RedirectUris:1"] = $"{AllowedOrigin}/oidc-silent-redirect.html",
            ["OpenIddict:Applications:0:PostLogoutRedirectUris:0"] = AllowedOrigin
        });
    }

    private static HttpClient CreateClient(TestApiFactory factory)
    {
        return factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = BaseUri
        });
    }
}
