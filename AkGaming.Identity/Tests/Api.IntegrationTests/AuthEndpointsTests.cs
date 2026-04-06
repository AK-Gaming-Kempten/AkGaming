using System.Net;
using System.Net.Http.Json;

namespace AkGaming.Identity.Api.IntegrationTests;

public sealed class AuthEndpointsTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;
    private readonly TestApiFactory _factory;

    public AuthEndpointsTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task Register_Login_Refresh_Works_EndToEnd()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var password = "Password123";

        var registerResponse = await _client.PostAsJsonAsync("/auth/register", new
        {
            Email = email,
            Password = password,
            PrivacyPolicyAccepted = true,
            Username = "Register User"
        });

        var registerBody = await registerResponse.Content.ReadAsStringAsync();
        Assert.True(registerResponse.StatusCode == HttpStatusCode.Forbidden, $"Expected 403, got {(int)registerResponse.StatusCode}: {registerBody}");

        var verificationIssueResponse = await _client.PostAsJsonAsync("/auth/email/send-verification", new
        {
            Email = email
        });

        var verificationIssueBody = await verificationIssueResponse.Content.ReadAsStringAsync();
        Assert.True(verificationIssueResponse.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)verificationIssueResponse.StatusCode}: {verificationIssueBody}");

        var verificationPayload = await verificationIssueResponse.Content.ReadFromJsonAsync<EmailVerificationPayload>();
        Assert.NotNull(verificationPayload);
        Assert.False(string.IsNullOrWhiteSpace(verificationPayload.VerificationToken));

        var verifyResponse = await _client.PostAsJsonAsync("/auth/email/verify", new
        {
            Token = verificationPayload.VerificationToken
        });

        var verifyBody = await verifyResponse.Content.ReadAsStringAsync();
        Assert.True(verifyResponse.StatusCode == HttpStatusCode.NoContent, $"Expected 204, got {(int)verifyResponse.StatusCode}: {verifyBody}");

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new
        {
            Email = email,
            Password = password
        });

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(loginResponse.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)loginResponse.StatusCode}: {loginBody}");

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<AuthPayload>();
        Assert.NotNull(loginPayload);
        Assert.False(string.IsNullOrWhiteSpace(loginPayload.RefreshToken));

        var refreshResponse = await _client.PostAsJsonAsync("/auth/refresh", new
        {
            RefreshToken = loginPayload.RefreshToken
        });

        var refreshBody = await refreshResponse.Content.ReadAsStringAsync();
        Assert.True(refreshResponse.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)refreshResponse.StatusCode}: {refreshBody}");

        var refreshPayload = await refreshResponse.Content.ReadFromJsonAsync<AuthPayload>();
        Assert.NotNull(refreshPayload);
        Assert.NotEqual(loginPayload.RefreshToken, refreshPayload.RefreshToken);
    }

    [Fact]
    public async Task Login_WithUnknownUser_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            Email = $"missing-{Guid.NewGuid():N}@example.com",
            Password = "Password123"
        });

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected 401, got {(int)response.StatusCode}: {body}");
    }

    [Fact]
    public async Task Register_WithoutPrivacyPolicyConsent_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            Email = $"user-{Guid.NewGuid():N}@example.com",
            Password = "Password123",
            PrivacyPolicyAccepted = false,
            Username = "Register User"
        });

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}: {body}");
    }

    [Fact]
    public async Task Logout_Get_WithAllowedExternalReturnUrl_Redirects()
    {
        using var noRedirectClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        var encodedReturnUrl = Uri.EscapeDataString("https://management.akgaming.de/");
        var response = await noRedirectClient.GetAsync($"/auth/logout?returnUrl={encodedReturnUrl}");

        Assert.True(response.StatusCode == HttpStatusCode.Redirect, $"Expected 302, got {(int)response.StatusCode}");
        Assert.Equal("https://management.akgaming.de/", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Logout_Get_WithDisallowedExternalReturnUrl_ReturnsBadRequest()
    {
        var encodedReturnUrl = Uri.EscapeDataString("https://evil.example.com/");
        var response = await _client.GetAsync($"/auth/logout?returnUrl={encodedReturnUrl}");

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400, got {(int)response.StatusCode}: {body}");
    }

    [Fact]
    public async Task EmailVerification_Request_WithoutAuthentication_Works()
    {
        var email = $"verify-{Guid.NewGuid():N}@example.com";
        var password = "Password123";

        var registerResponse = await _client.PostAsJsonAsync("/auth/register", new
        {
            Email = email,
            Password = password,
            PrivacyPolicyAccepted = true,
            Username = "Verify User"
        });

        var registerBody = await registerResponse.Content.ReadAsStringAsync();
        Assert.True(registerResponse.StatusCode == HttpStatusCode.Forbidden, $"Expected 403, got {(int)registerResponse.StatusCode}: {registerBody}");

        var response = await _client.PostAsJsonAsync("/auth/email/send-verification", new
        {
            Email = email
        });

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}: {body}");
    }

    private sealed record AuthPayload(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken);
    private sealed record EmailVerificationPayload(string Message, string? VerificationToken);
}
