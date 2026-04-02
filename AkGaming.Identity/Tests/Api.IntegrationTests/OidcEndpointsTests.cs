using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace AkGaming.Identity.Api.IntegrationTests;

public sealed class OidcEndpointsTests : IClassFixture<TestApiFactory>
{
    private static readonly Uri BaseUri = new("https://localhost");
    private readonly TestApiFactory _factory;

    public OidcEndpointsTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuthorizationCodeFlow_WithPkce_ReturnsTokens_AndUserInfo()
    {
        using var client = CreateNoRedirectClient();

        var pkce = PkceState.Create();
        var authorizeUrl = BuildAuthorizeUrl(
            clientId: "test-public-client",
            redirectUri: "https://app.akgaming.de/callback",
            scopes: "openid profile email roles offline_access",
            state: "state-public",
            pkce);

        var authorizeResponse = await client.GetAsync(authorizeUrl);
        Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);
        Assert.StartsWith("/account/login", authorizeResponse.Headers.Location?.ToString(), StringComparison.Ordinal);

        var registerLocation = authorizeResponse.Headers.Location?.ToString() ?? throw new InvalidOperationException("Missing login redirect.");
        var registerReturnUrl = QueryHelpers.ParseQuery(new Uri(BaseUri, registerLocation).Query)["returnUrl"].ToString();
        await RegisterInteractiveAsync(client, registerReturnUrl, $"oidc-{Guid.NewGuid():N}@example.com");

        var resumeAuthorizeResponse = await client.GetAsync(registerReturnUrl);
        Assert.Equal(HttpStatusCode.Redirect, resumeAuthorizeResponse.StatusCode);
        Assert.StartsWith("https://app.akgaming.de/callback", resumeAuthorizeResponse.Headers.Location?.ToString(), StringComparison.Ordinal);

        var callbackUri = resumeAuthorizeResponse.Headers.Location ?? throw new InvalidOperationException("Missing callback redirect.");
        var callbackQuery = QueryHelpers.ParseQuery(callbackUri.Query);
        var code = callbackQuery["code"].ToString();
        Assert.False(string.IsNullOrWhiteSpace(code));
        Assert.Equal("state-public", callbackQuery["state"].ToString());

        var tokenResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = "test-public-client",
            ["redirect_uri"] = "https://app.akgaming.de/callback",
            ["code"] = code,
            ["code_verifier"] = pkce.Verifier
        }!));

        var tokenBody = await tokenResponse.Content.ReadAsStringAsync();
        Assert.True(tokenResponse.IsSuccessStatusCode, $"Expected successful token exchange: {tokenBody}");

        using var tokenJson = JsonDocument.Parse(tokenBody);
        var accessToken = tokenJson.RootElement.GetProperty("access_token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.True(tokenJson.RootElement.TryGetProperty("refresh_token", out _));

        using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var userInfoResponse = await client.SendAsync(userInfoRequest);

        var userInfoBody = await userInfoResponse.Content.ReadAsStringAsync();
        Assert.True(userInfoResponse.IsSuccessStatusCode, $"Expected successful userinfo response: {userInfoBody}");

        using var userInfoJson = JsonDocument.Parse(userInfoBody);
        Assert.True(userInfoJson.RootElement.TryGetProperty("sub", out _));
        Assert.True(userInfoJson.RootElement.TryGetProperty("email", out _));
    }

    [Fact]
    public async Task AuthorizationCodeFlow_WithExplicitConsent_RedirectsToConsent_ThenReturnsCode()
    {
        using var client = CreateNoRedirectClient();

        var registerReturnUrl = "/account/manage";
        await RegisterInteractiveAsync(client, registerReturnUrl, $"explicit-{Guid.NewGuid():N}@example.com");

        var pkce = PkceState.Create();
        var authorizeUrl = BuildAuthorizeUrl(
            clientId: "test-explicit-client",
            redirectUri: "https://explicit.akgaming.de/callback",
            scopes: "openid profile email roles offline_access",
            state: "state-explicit",
            pkce);

        var authorizeResponse = await client.GetAsync(authorizeUrl);
        Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);
        Assert.StartsWith("/account/consent", authorizeResponse.Headers.Location?.ToString(), StringComparison.Ordinal);

        var consentUrl = authorizeResponse.Headers.Location?.ToString() ?? throw new InvalidOperationException("Missing consent redirect.");
        var consentPageResponse = await client.GetAsync(consentUrl);
        var consentHtml = await consentPageResponse.Content.ReadAsStringAsync();
        Assert.True(consentPageResponse.IsSuccessStatusCode, $"Expected consent page: {consentHtml}");
        Assert.Contains("Authorize Application", consentHtml, StringComparison.Ordinal);

        var consentQuery = QueryHelpers.ParseQuery(new Uri(BaseUri, consentUrl).Query);
        var consentReturnUrl = consentQuery["returnUrl"].ToString();
        var consentToken = ExtractAntiForgeryToken(consentHtml);

        var approveResponse = await PostFormAsync(
            client,
            "/account/consent?handler=Approve",
            new Dictionary<string, string?>
            {
                ["ReturnUrl"] = consentReturnUrl,
                ["__RequestVerificationToken"] = consentToken
            });
        Assert.Equal(HttpStatusCode.Redirect, approveResponse.StatusCode);

        var resumeAuthorizeResponse = await client.GetAsync(approveResponse.Headers.Location);
        Assert.Equal(HttpStatusCode.Redirect, resumeAuthorizeResponse.StatusCode);
        Assert.StartsWith("https://explicit.akgaming.de/callback", resumeAuthorizeResponse.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizationCodeFlow_WithExistingConsent_DoesNotPromptAgain()
    {
        using var client = CreateNoRedirectClient();

        await RegisterInteractiveAsync(client, "/account/manage", $"remembered-{Guid.NewGuid():N}@example.com");

        var firstPkce = PkceState.Create();
        var firstAuthorizeUrl = BuildAuthorizeUrl(
            clientId: "test-explicit-client",
            redirectUri: "https://explicit.akgaming.de/callback",
            scopes: "openid profile email roles offline_access",
            state: "state-first",
            pkce: firstPkce);

        var firstAuthorizeResponse = await client.GetAsync(firstAuthorizeUrl);
        Assert.Equal(HttpStatusCode.Redirect, firstAuthorizeResponse.StatusCode);
        Assert.StartsWith("/account/consent", firstAuthorizeResponse.Headers.Location?.ToString(), StringComparison.Ordinal);

        var consentUrl = firstAuthorizeResponse.Headers.Location?.ToString() ?? throw new InvalidOperationException("Missing consent redirect.");
        var consentPageResponse = await client.GetAsync(consentUrl);
        var consentHtml = await consentPageResponse.Content.ReadAsStringAsync();
        var consentToken = ExtractAntiForgeryToken(consentHtml);
        var consentQuery = QueryHelpers.ParseQuery(new Uri(BaseUri, consentUrl).Query);
        var consentReturnUrl = consentQuery["returnUrl"].ToString();

        var approveResponse = await PostFormAsync(
            client,
            "/account/consent?handler=Approve",
            new Dictionary<string, string?>
            {
                ["ReturnUrl"] = consentReturnUrl,
                ["__RequestVerificationToken"] = consentToken
            });
        Assert.Equal(HttpStatusCode.Redirect, approveResponse.StatusCode);

        var firstCallbackResponse = await client.GetAsync(approveResponse.Headers.Location);
        Assert.Equal(HttpStatusCode.Redirect, firstCallbackResponse.StatusCode);
        Assert.StartsWith("https://explicit.akgaming.de/callback", firstCallbackResponse.Headers.Location?.ToString(), StringComparison.Ordinal);

        var secondPkce = PkceState.Create();
        var secondAuthorizeUrl = BuildAuthorizeUrl(
            clientId: "test-explicit-client",
            redirectUri: "https://explicit.akgaming.de/callback",
            scopes: "openid profile email roles offline_access",
            state: "state-second",
            pkce: secondPkce);

        var secondAuthorizeResponse = await client.GetAsync(secondAuthorizeUrl);
        Assert.Equal(HttpStatusCode.Redirect, secondAuthorizeResponse.StatusCode);
        Assert.StartsWith("https://explicit.akgaming.de/callback", secondAuthorizeResponse.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task EndSession_LogsOutAndRedirectsToRegisteredPostLogoutUri()
    {
        using var client = CreateNoRedirectClient();

        await RegisterInteractiveAsync(client, "/account/manage", $"logout-{Guid.NewGuid():N}@example.com");

        var logoutUrl = QueryHelpers.AddQueryString(
            "/connect/logout",
            new Dictionary<string, string?>
            {
                ["client_id"] = "test-public-client",
                ["post_logout_redirect_uri"] = "https://app.akgaming.de/logout-callback"
            }!);

        var logoutResponse = await client.GetAsync(logoutUrl);
        Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);
        Assert.Equal("https://app.akgaming.de/logout-callback", logoutResponse.Headers.Location?.ToString());

        var manageResponse = await client.GetAsync("/account/manage");
        Assert.Equal(HttpStatusCode.Redirect, manageResponse.StatusCode);
        Assert.Contains("/account/login", manageResponse.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DiscordLogin_LocalFlow_SignsInCookieSession_AndRedirectsToReturnUrl()
    {
        using var client = CreateNoRedirectClient();

        var startResponse = await client.GetAsync("/auth/discord/start?return_url=%2Faccount%2Fmanage");
        Assert.Equal(HttpStatusCode.Redirect, startResponse.StatusCode);
        Assert.StartsWith("https://discord.test/authorize", startResponse.Headers.Location?.ToString(), StringComparison.Ordinal);

        var state = QueryHelpers.ParseQuery(startResponse.Headers.Location?.Query ?? string.Empty)["state"].ToString();
        Assert.False(string.IsNullOrWhiteSpace(state));

        var callbackResponse = await client.GetAsync($"/auth/discord/callback?code=LocalLoginFlow&state={Uri.EscapeDataString(state)}");
        Assert.Equal(HttpStatusCode.Redirect, callbackResponse.StatusCode);
        Assert.Equal("/account/manage", callbackResponse.Headers.Location?.ToString());

        var manageResponse = await client.GetAsync("/account/manage");
        var manageHtml = await manageResponse.Content.ReadAsStringAsync();
        Assert.True(manageResponse.IsSuccessStatusCode, $"Expected account page after Discord login: {manageHtml}");
        Assert.Contains("localloginflow@example.com", manageHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("discord-LocalLoginFlow", manageHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DiscordLink_LocalSessionFlow_LinksDiscordAccount_AndReturnsToManage()
    {
        using var client = CreateNoRedirectClient();

        await RegisterInteractiveAsync(client, "/account/manage", $"discord-link-{Guid.NewGuid():N}@example.com");

        var managePageResponse = await client.GetAsync("/account/manage");
        var managePageHtml = await managePageResponse.Content.ReadAsStringAsync();
        Assert.True(managePageResponse.IsSuccessStatusCode, $"Expected manage page before link flow: {managePageHtml}");

        var antiForgeryToken = ExtractAntiForgeryToken(managePageHtml);
        var startLinkResponse = await PostFormAsync(
            client,
            "/account/manage?handler=StartDiscordLink",
            new Dictionary<string, string?>
            {
                ["__RequestVerificationToken"] = antiForgeryToken
            });
        Assert.Equal(HttpStatusCode.Redirect, startLinkResponse.StatusCode);
        Assert.StartsWith("https://discord.test/authorize", startLinkResponse.Headers.Location?.ToString(), StringComparison.Ordinal);

        var state = QueryHelpers.ParseQuery(startLinkResponse.Headers.Location?.Query ?? string.Empty)["state"].ToString();
        Assert.False(string.IsNullOrWhiteSpace(state));

        var callbackResponse = await client.GetAsync($"/auth/discord/callback?code=LinkFlow&state={Uri.EscapeDataString(state)}");
        Assert.Equal(HttpStatusCode.Redirect, callbackResponse.StatusCode);
        Assert.Equal("/account/manage", callbackResponse.Headers.Location?.ToString());

        var linkedManageResponse = await client.GetAsync("/account/manage");
        var linkedManageHtml = await linkedManageResponse.Content.ReadAsStringAsync();
        Assert.True(linkedManageResponse.IsSuccessStatusCode, $"Expected manage page after link flow: {linkedManageHtml}");
        Assert.Contains("discord-LinkFlow", linkedManageHtml, StringComparison.Ordinal);
    }

    private HttpClient CreateNoRedirectClient()
    {
        return _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = BaseUri,
            AllowAutoRedirect = false
        });
    }

    private static string BuildAuthorizeUrl(string clientId, string redirectUri, string scopes, string state, PkceState pkce)
    {
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = scopes,
            ["state"] = state,
            ["code_challenge"] = pkce.Challenge,
            ["code_challenge_method"] = "S256"
        };

        return QueryHelpers.AddQueryString("/connect/authorize", query!);
    }

    private static async Task RegisterInteractiveAsync(HttpClient client, string returnUrl, string email)
    {
        var registerPageResponse = await client.GetAsync($"/account/register?returnUrl={Uri.EscapeDataString(returnUrl)}");
        var registerPageHtml = await registerPageResponse.Content.ReadAsStringAsync();
        Assert.True(registerPageResponse.IsSuccessStatusCode, $"Expected register page: {registerPageHtml}");

        var formFields = new Dictionary<string, string?>
        {
            ["ReturnUrl"] = returnUrl,
            ["Email"] = email,
            ["Password"] = "Password123",
            ["PrivacyPolicyAccepted"] = "true"
        };

        var token = ExtractAntiForgeryToken(registerPageHtml);
        if (!string.IsNullOrWhiteSpace(token))
        {
            formFields["__RequestVerificationToken"] = token;
        }

        var registerResponse = await PostFormAsync(client, "/account/register", formFields);
        var registerBody = await registerResponse.Content.ReadAsStringAsync();
        Assert.True(registerResponse.StatusCode == HttpStatusCode.Redirect, $"Expected redirect after register, got {(int)registerResponse.StatusCode}: {registerBody}");
    }

    private static async Task<HttpResponseMessage> PostFormAsync(HttpClient client, string url, Dictionary<string, string?> formFields)
    {
        var filtered = formFields
            .Where(x => x.Value is not null)
            .ToDictionary(x => x.Key, x => x.Value!);

        return await client.PostAsync(url, new FormUrlEncodedContent(filtered));
    }

    private static string? ExtractAntiForgeryToken(string html)
    {
        const string marker = "name=\"__RequestVerificationToken\"";
        var markerIndex = html.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return null;
        }

        var valueMarker = "value=\"";
        var valueIndex = html.IndexOf(valueMarker, markerIndex, StringComparison.Ordinal);
        if (valueIndex < 0)
        {
            return null;
        }

        valueIndex += valueMarker.Length;
        var endIndex = html.IndexOf('"', valueIndex);
        return endIndex < 0 ? null : html[valueIndex..endIndex];
    }

    private sealed record PkceState(string Verifier, string Challenge)
    {
        public static PkceState Create()
        {
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);
            var verifier = WebEncoders.Base64UrlEncode(bytes);
            var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
            var challenge = WebEncoders.Base64UrlEncode(hash);
            return new PkceState(verifier, challenge);
        }
    }
}
