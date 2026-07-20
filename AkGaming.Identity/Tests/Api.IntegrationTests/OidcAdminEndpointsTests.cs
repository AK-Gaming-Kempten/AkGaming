using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AkGaming.Identity.Contracts.Auth;
using AkGaming.Identity.Domain.Constants;
using AkGaming.Identity.Domain.Entities;
using AkGaming.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Identity.Api.IntegrationTests;

public sealed class OidcAdminEndpointsTests
{
    private static readonly Uri BaseUri = new("https://localhost");

    [Fact]
    public async Task ProtectedClient_CannotBeUpdatedOrDeleted()
    {
        using var factory = CreateFactory();
        using var client = CreateNoRedirectClient(factory);
        var accessToken = await AuthenticateAdminAsync(factory, client, $"protected-client-{Guid.NewGuid():N}@example.com");

        using var listRequest = CreateAdminRequest(HttpMethod.Get, "/admin/oidc/clients", accessToken);
        using var listResponse = await client.SendAsync(listRequest);
        listResponse.EnsureSuccessStatusCode();

        var clients = await listResponse.Content.ReadFromJsonAsync<List<OidcClientResponse>>();
        var protectedClient = Assert.Single(clients!, candidate => candidate.ClientId == "test-public-client");
        Assert.True(protectedClient.IsProtected);

        using var updateRequest = CreateAdminRequest(HttpMethod.Put, "/admin/oidc/clients/test-public-client", accessToken);
        updateRequest.Content = JsonContent.Create(new AdminUpdateOidcClientRequest(
            DisplayName: "Modified",
            ClientType: "public",
            ConsentType: "implicit",
            RequirePkce: true,
            AllowAuthorizationCodeFlow: true,
            AllowClientCredentialsFlow: false,
            AllowRefreshTokenFlow: true,
            NewClientSecret: null,
            RedirectUris: ["https://app.akgaming.de/callback"],
            PostLogoutRedirectUris: ["https://app.akgaming.de/logout-callback"],
            Scopes: ["openid", "profile", "email", "roles", "offline_access", "management_api"]));
        using var updateResponse = await client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);

        using var deleteRequest = CreateAdminRequest(HttpMethod.Delete, "/admin/oidc/clients/test-public-client", accessToken);
        using var deleteResponse = await client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task OidcClient_CanBeCreatedUpdatedAndDeleted()
    {
        using var factory = CreateFactory();
        using var client = CreateNoRedirectClient(factory);
        var accessToken = await AuthenticateAdminAsync(factory, client, $"mutable-client-{Guid.NewGuid():N}@example.com");

        const string scopeName = "members_portal";
        const string clientId = "members-portal";

        await CreateScopeAsync(client, accessToken, new AdminCreateOidcScopeRequest(
            Name: scopeName,
            DisplayName: "Members Portal",
            Description: "Portal access",
            Resources: [scopeName]));

        var createdClient = await CreateClientAsync(client, accessToken, new AdminCreateOidcClientRequest(
            ClientId: clientId,
            ClientSecret: "members-secret",
            DisplayName: "Members Portal Frontend",
            ClientType: "confidential",
            ConsentType: "explicit",
            RequirePkce: true,
            AllowAuthorizationCodeFlow: true,
            AllowClientCredentialsFlow: false,
            AllowRefreshTokenFlow: true,
            RedirectUris: ["https://members.akgaming.de/signin-oidc"],
            PostLogoutRedirectUris: ["https://members.akgaming.de/signout-callback-oidc"],
            Scopes: ["openid", "profile", scopeName]));

        Assert.Equal(clientId, createdClient.ClientId);
        Assert.False(createdClient.IsProtected);
        Assert.Contains(scopeName, createdClient.Scopes);

        var updatedRequest = new AdminUpdateOidcClientRequest(
            DisplayName: "Members Portal Admin",
            ClientType: "public",
            ConsentType: "implicit",
            RequirePkce: true,
            AllowAuthorizationCodeFlow: true,
            AllowClientCredentialsFlow: false,
            AllowRefreshTokenFlow: false,
            NewClientSecret: null,
            RedirectUris: ["https://members.akgaming.de/signin-oidc"],
            PostLogoutRedirectUris: ["https://members.akgaming.de/signout-callback-oidc"],
            Scopes: ["openid", "profile", "email", scopeName]);

        using var updateRequest = CreateAdminRequest(HttpMethod.Put, $"/admin/oidc/clients/{clientId}", accessToken);
        updateRequest.Content = JsonContent.Create(updatedRequest);
        using var updateResponse = await client.SendAsync(updateRequest);
        updateResponse.EnsureSuccessStatusCode();

        var updatedClient = await updateResponse.Content.ReadFromJsonAsync<OidcClientResponse>();
        Assert.NotNull(updatedClient);
        Assert.Equal("Members Portal Admin", updatedClient!.DisplayName);
        Assert.Equal("public", updatedClient.ClientType);
        Assert.False(updatedClient.AllowRefreshTokenFlow);
        Assert.Contains("email", updatedClient.Scopes);

        using var deleteRequest = CreateAdminRequest(HttpMethod.Delete, $"/admin/oidc/clients/{clientId}", accessToken);
        using var deleteResponse = await client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var getDeletedRequest = CreateAdminRequest(HttpMethod.Get, $"/admin/oidc/clients/{clientId}", accessToken);
        using var getDeletedResponse = await client.SendAsync(getDeletedRequest);
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    [Description("Creates a confidential machine-to-machine client and verifies that it can obtain a scoped client-credentials token.")]
    public async Task ServiceClient_CanBeCreatedAndUseClientCredentialsFlow()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = CreateNoRedirectClient(factory);
        var accessToken = await AuthenticateAdminAsync(factory, client, $"service-client-{Guid.NewGuid():N}@example.com");
        const string scopeName = "identity_discord_links";
        const string clientId = "service-automation-client";
        const string clientSecret = "service-automation-secret";

        // Act
        var createdClient = await CreateClientAsync(client, accessToken, new AdminCreateOidcClientRequest(
            ClientId: clientId,
            ClientSecret: clientSecret,
            DisplayName: "Service Automation Client",
            ClientType: "confidential",
            ConsentType: "implicit",
            RequirePkce: false,
            AllowAuthorizationCodeFlow: false,
            AllowClientCredentialsFlow: true,
            AllowRefreshTokenFlow: false,
            RedirectUris: [],
            PostLogoutRedirectUris: [],
            Scopes: [scopeName]));
        using var updateRequest = CreateAdminRequest(HttpMethod.Put, $"/admin/oidc/clients/{clientId}", accessToken);
        updateRequest.Content = JsonContent.Create(new AdminUpdateOidcClientRequest(
            DisplayName: "Updated Service Automation Client",
            ClientType: "confidential",
            ConsentType: "implicit",
            RequirePkce: false,
            AllowAuthorizationCodeFlow: false,
            AllowClientCredentialsFlow: true,
            AllowRefreshTokenFlow: false,
            NewClientSecret: null,
            RedirectUris: [],
            PostLogoutRedirectUris: [],
            Scopes: [scopeName]));
        using var updateResponse = await client.SendAsync(updateRequest);
        updateResponse.EnsureSuccessStatusCode();
        var updatedClient = await updateResponse.Content.ReadFromJsonAsync<OidcClientResponse>();
        using var tokenResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["scope"] = scopeName
        }));
        tokenResponse.EnsureSuccessStatusCode();
        using var tokenDocument = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        var serviceAccessToken = tokenDocument.RootElement.GetProperty("access_token").GetString();
        using var protectedRequest = new HttpRequestMessage(HttpMethod.Get, $"/internal/discord-links/{Guid.NewGuid()}");
        protectedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceAccessToken);
        using var protectedResponse = await client.SendAsync(protectedRequest);

        // Assert
        Assert.False(createdClient.AllowAuthorizationCodeFlow);
        Assert.True(createdClient.AllowClientCredentialsFlow);
        Assert.False(createdClient.AllowRefreshTokenFlow);
        Assert.False(createdClient.RequirePkce);
        Assert.Empty(createdClient.RedirectUris);
        Assert.NotNull(updatedClient);
        Assert.Equal("Updated Service Automation Client", updatedClient!.DisplayName);
        Assert.True(updatedClient.AllowClientCredentialsFlow);
        Assert.False(string.IsNullOrWhiteSpace(serviceAccessToken));
        Assert.Equal(HttpStatusCode.NotFound, protectedResponse.StatusCode);
    }

    [Fact]
    public async Task ConfidentialOidcClient_CanBeUpdatedWithoutReplacingSecret()
    {
        using var factory = CreateFactory();
        using var client = CreateNoRedirectClient(factory);
        var accessToken = await AuthenticateAdminAsync(factory, client, $"confidential-update-{Guid.NewGuid():N}@example.com");

        const string scopeName = "tournaments_api";
        const string clientId = "tournaments-frontend";

        await CreateScopeAsync(client, accessToken, new AdminCreateOidcScopeRequest(
            Name: scopeName,
            DisplayName: "Tournaments API",
            Description: "Tournament backend access",
            Resources: [scopeName]));

        await CreateClientAsync(client, accessToken, new AdminCreateOidcClientRequest(
            ClientId: clientId,
            ClientSecret: "tournaments-secret",
            DisplayName: "Tournaments Frontend",
            ClientType: "confidential",
            ConsentType: "explicit",
            RequirePkce: true,
            AllowAuthorizationCodeFlow: true,
            AllowClientCredentialsFlow: false,
            AllowRefreshTokenFlow: true,
            RedirectUris: ["https://tournaments.akgaming.de/signin-oidc"],
            PostLogoutRedirectUris: ["https://tournaments.akgaming.de/signout-callback-oidc"],
            Scopes: ["openid", "profile", "email", "offline_access", scopeName]));

        using var updateRequest = CreateAdminRequest(HttpMethod.Put, $"/admin/oidc/clients/{clientId}", accessToken);
        updateRequest.Content = JsonContent.Create(new AdminUpdateOidcClientRequest(
            DisplayName: "Tournaments Frontend Updated",
            ClientType: "confidential",
            ConsentType: "explicit",
            RequirePkce: true,
            AllowAuthorizationCodeFlow: true,
            AllowClientCredentialsFlow: false,
            AllowRefreshTokenFlow: true,
            NewClientSecret: null,
            RedirectUris: ["https://tournaments.akgaming.de/signin-oidc"],
            PostLogoutRedirectUris: ["https://tournaments.akgaming.de/signout-callback-oidc"],
            Scopes: ["openid", "profile", "email", "offline_access", scopeName]));

        using var updateResponse = await client.SendAsync(updateRequest);
        updateResponse.EnsureSuccessStatusCode();

        var updatedClient = await updateResponse.Content.ReadFromJsonAsync<OidcClientResponse>();
        Assert.NotNull(updatedClient);
        Assert.Equal("Tournaments Frontend Updated", updatedClient!.DisplayName);
        Assert.Equal("confidential", updatedClient.ClientType);
        Assert.True(updatedClient.HasClientSecret);
    }

    [Fact]
    public async Task ConfidentialOidcClient_CanRotateSecret()
    {
        using var factory = CreateFactory();
        using var client = CreateNoRedirectClient(factory);
        var accessToken = await AuthenticateAdminAsync(factory, client, $"confidential-rotate-{Guid.NewGuid():N}@example.com");

        const string scopeName = "billing_api";
        const string clientId = "billing-frontend";

        await CreateScopeAsync(client, accessToken, new AdminCreateOidcScopeRequest(
            Name: scopeName,
            DisplayName: "Billing API",
            Description: "Billing backend access",
            Resources: [scopeName]));

        await CreateClientAsync(client, accessToken, new AdminCreateOidcClientRequest(
            ClientId: clientId,
            ClientSecret: "billing-secret-old",
            DisplayName: "Billing Frontend",
            ClientType: "confidential",
            ConsentType: "explicit",
            RequirePkce: true,
            AllowAuthorizationCodeFlow: true,
            AllowClientCredentialsFlow: false,
            AllowRefreshTokenFlow: true,
            RedirectUris: ["https://billing.akgaming.de/signin-oidc"],
            PostLogoutRedirectUris: ["https://billing.akgaming.de/signout-callback-oidc"],
            Scopes: ["openid", "profile", "email", "offline_access", scopeName]));

        using var updateRequest = CreateAdminRequest(HttpMethod.Put, $"/admin/oidc/clients/{clientId}", accessToken);
        updateRequest.Content = JsonContent.Create(new AdminUpdateOidcClientRequest(
            DisplayName: "Billing Frontend Updated",
            ClientType: "confidential",
            ConsentType: "explicit",
            RequirePkce: true,
            AllowAuthorizationCodeFlow: true,
            AllowClientCredentialsFlow: false,
            AllowRefreshTokenFlow: true,
            NewClientSecret: "billing-secret-new",
            RedirectUris: ["https://billing.akgaming.de/signin-oidc"],
            PostLogoutRedirectUris: ["https://billing.akgaming.de/signout-callback-oidc"],
            Scopes: ["openid", "profile", "email", "offline_access", scopeName]));

        using var updateResponse = await client.SendAsync(updateRequest);
        updateResponse.EnsureSuccessStatusCode();

        var updatedClient = await updateResponse.Content.ReadFromJsonAsync<OidcClientResponse>();
        Assert.NotNull(updatedClient);
        Assert.Equal("Billing Frontend Updated", updatedClient!.DisplayName);
        Assert.Equal("confidential", updatedClient.ClientType);
        Assert.True(updatedClient.HasClientSecret);
    }

    [Fact]
    public async Task ProtectedScope_CannotBeUpdatedOrDeleted()
    {
        using var factory = CreateFactory();
        using var client = CreateNoRedirectClient(factory);
        var accessToken = await AuthenticateAdminAsync(factory, client, $"protected-scope-{Guid.NewGuid():N}@example.com");

        using var getRequest = CreateAdminRequest(HttpMethod.Get, "/admin/oidc/scopes/management_api", accessToken);
        using var getResponse = await client.SendAsync(getRequest);
        getResponse.EnsureSuccessStatusCode();

        var scope = await getResponse.Content.ReadFromJsonAsync<OidcScopeResponse>();
        Assert.NotNull(scope);
        Assert.True(scope!.IsProtected);

        using var updateRequest = CreateAdminRequest(HttpMethod.Put, "/admin/oidc/scopes/management_api", accessToken);
        updateRequest.Content = JsonContent.Create(new AdminUpdateOidcScopeRequest(
            DisplayName: "Changed",
            Description: "Changed",
            Resources: ["management_api"]));
        using var updateResponse = await client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);

        using var deleteRequest = CreateAdminRequest(HttpMethod.Delete, "/admin/oidc/scopes/management_api", accessToken);
        using var deleteResponse = await client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task OidcScope_CanBeCreatedUpdatedAndDeleted_WhenNotAssignedToAClient()
    {
        using var factory = CreateFactory();
        using var client = CreateNoRedirectClient(factory);
        var accessToken = await AuthenticateAdminAsync(factory, client, $"mutable-scope-{Guid.NewGuid():N}@example.com");

        const string scopeName = "billing_api";

        var createdScope = await CreateScopeAsync(client, accessToken, new AdminCreateOidcScopeRequest(
            Name: scopeName,
            DisplayName: "Billing API",
            Description: "Billing access",
            Resources: [scopeName]));
        Assert.Equal(scopeName, createdScope.Name);
        Assert.False(createdScope.IsProtected);

        using var updateRequest = CreateAdminRequest(HttpMethod.Put, $"/admin/oidc/scopes/{scopeName}", accessToken);
        updateRequest.Content = JsonContent.Create(new AdminUpdateOidcScopeRequest(
            DisplayName: "Billing API Access",
            Description: "Billing access for external integrations",
            Resources: [scopeName, "billing_read_model"]));
        using var updateResponse = await client.SendAsync(updateRequest);
        updateResponse.EnsureSuccessStatusCode();

        var updatedScope = await updateResponse.Content.ReadFromJsonAsync<OidcScopeResponse>();
        Assert.NotNull(updatedScope);
        Assert.Equal("Billing API Access", updatedScope!.DisplayName);
        Assert.Contains("billing_read_model", updatedScope.Resources);

        using var deleteRequest = CreateAdminRequest(HttpMethod.Delete, $"/admin/oidc/scopes/{scopeName}", accessToken);
        using var deleteResponse = await client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var getDeletedRequest = CreateAdminRequest(HttpMethod.Get, $"/admin/oidc/scopes/{scopeName}", accessToken);
        using var getDeletedResponse = await client.SendAsync(getDeletedRequest);
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    private static TestApiFactory CreateFactory()
    {
        return new TestApiFactory();
    }

    private static HttpClient CreateNoRedirectClient(TestApiFactory factory)
    {
        return factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = BaseUri,
            AllowAutoRedirect = false
        });
    }

    private async Task<string> AuthenticateAdminAsync(TestApiFactory factory, HttpClient client, string email)
    {
        await RegisterInteractiveAsync(client, "/account/manage", email);
        await VerifyEmailAsync(client, email);
        await PromoteUserToAdminAsync(factory, email);

        var pkce = PkceState.Create();
        var authorizeUrl = BuildAuthorizeUrl(
            clientId: "test-public-client",
            redirectUri: "https://app.akgaming.de/callback",
            scopes: "openid profile email roles management_api",
            state: $"state-{Guid.NewGuid():N}",
            pkce);

        var authorizeResponse = await client.GetAsync(authorizeUrl);
        Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);
        Assert.StartsWith("https://app.akgaming.de/callback", authorizeResponse.Headers.Location?.ToString(), StringComparison.Ordinal);

        var callbackUri = authorizeResponse.Headers.Location ?? throw new InvalidOperationException("Missing callback redirect.");
        var callbackQuery = QueryHelpers.ParseQuery(callbackUri.Query);
        var code = callbackQuery["code"].ToString();
        Assert.False(string.IsNullOrWhiteSpace(code));

        using var tokenResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string?>
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
        return accessToken!;
    }

    private static async Task PromoteUserToAdminAsync(TestApiFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var user = dbContext.Users
            .Single(candidate => candidate.Email == email);

        var adminRole = dbContext.Roles.SingleOrDefault(role => role.Name == RoleNames.Admin);
        if (adminRole is null)
        {
            adminRole = new Role { Name = RoleNames.Admin };
            dbContext.Roles.Add(adminRole);
        }

        var existingLink = dbContext.UserRoles.SingleOrDefault(link => link.UserId == user.Id && link.RoleId == adminRole.Id);
        if (existingLink is null)
        {
            dbContext.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = adminRole.Id
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task RegisterInteractiveAsync(HttpClient client, string returnUrl, string email, string? username = null)
    {
        var registerPageResponse = await client.GetAsync($"/account/register?returnUrl={Uri.EscapeDataString(returnUrl)}");
        var registerPageHtml = await registerPageResponse.Content.ReadAsStringAsync();
        Assert.True(registerPageResponse.IsSuccessStatusCode, $"Expected register page: {registerPageHtml}");

        var formFields = new Dictionary<string, string?>
        {
            ["ReturnUrl"] = returnUrl,
            ["Email"] = email,
            ["Username"] = username ?? $"User {Guid.NewGuid():N}",
            ["Password"] = "Password123",
            ["PrivacyPolicyAccepted"] = "true"
        };

        var token = ExtractAntiForgeryToken(registerPageHtml);
        if (!string.IsNullOrWhiteSpace(token))
            formFields["__RequestVerificationToken"] = token;

        using var registerResponse = await PostFormAsync(client, "/account/register", formFields);
        var registerBody = await registerResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Redirect, registerResponse.StatusCode);
        Assert.True(string.IsNullOrWhiteSpace(registerBody) || registerResponse.Headers.Location is not null);
    }

    private static async Task VerifyEmailAsync(HttpClient client, string email)
    {
        using var issueResponse = await client.PostAsJsonAsync("/auth/email/send-verification", new
        {
            Email = email
        });
        var issueBody = await issueResponse.Content.ReadAsStringAsync();
        Assert.True(issueResponse.IsSuccessStatusCode, $"Expected verification token issuance: {issueBody}");

        using var issuePayload = JsonDocument.Parse(issueBody);
        var token = issuePayload.RootElement.GetProperty("verificationToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        using var verifyResponse = await client.PostAsJsonAsync("/auth/email/verify", new
        {
            Token = token
        });
        var verifyBody = await verifyResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.NoContent, verifyResponse.StatusCode);
        Assert.True(string.IsNullOrWhiteSpace(verifyBody));
    }

    private static async Task<OidcScopeResponse> CreateScopeAsync(HttpClient client, string accessToken, AdminCreateOidcScopeRequest request)
    {
        using var createRequest = CreateAdminRequest(HttpMethod.Post, "/admin/oidc/scopes", accessToken);
        createRequest.Content = JsonContent.Create(request);
        using var createResponse = await client.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();

        var scope = await createResponse.Content.ReadFromJsonAsync<OidcScopeResponse>();
        return scope ?? throw new InvalidOperationException("Expected scope payload.");
    }

    private static async Task<OidcClientResponse> CreateClientAsync(HttpClient client, string accessToken, AdminCreateOidcClientRequest request)
    {
        using var createRequest = CreateAdminRequest(HttpMethod.Post, "/admin/oidc/clients", accessToken);
        createRequest.Content = JsonContent.Create(request);
        using var createResponse = await client.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();

        var oidcClient = await createResponse.Content.ReadFromJsonAsync<OidcClientResponse>();
        return oidcClient ?? throw new InvalidOperationException("Expected client payload.");
    }

    private static HttpRequestMessage CreateAdminRequest(HttpMethod method, string url, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
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
            return null;

        var valueMarker = "value=\"";
        var valueIndex = html.IndexOf(valueMarker, markerIndex, StringComparison.Ordinal);
        if (valueIndex < 0)
            return null;

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
