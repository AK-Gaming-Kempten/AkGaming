using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AkGaming.Identity.Application.Abstractions;

namespace AkGaming.Identity.Api.IntegrationTests;

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"identity-integration-{Guid.NewGuid():N}.db");

    public TestApiFactory()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:IdentityDb", $"Data Source={_dbPath}");
        builder.UseSetting("AllowedHosts", "*");
        builder.UseSetting("Jwt:SecretKey", "integration-tests-secret-key-1234567890-abcdefghij");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:IdentityDb"] = $"Data Source={_dbPath}",
                ["Jwt:SecretKey"] = "integration-tests-secret-key-1234567890-abcdefghij",
                ["AllowedHosts"] = "*",
                ["Bridge:AllowedRedirectUris:0"] = "https://management.akgaming.de",
                ["OpenIddict:Issuer"] = "https://localhost",
                ["OpenIddict:Applications:0:ClientId"] = "test-public-client",
                ["OpenIddict:Applications:0:DisplayName"] = "Test Public Client",
                ["OpenIddict:Applications:0:ConsentType"] = "implicit",
                ["OpenIddict:Applications:0:ClientType"] = "public",
                ["OpenIddict:Applications:0:RequirePkce"] = "true",
                ["OpenIddict:Applications:0:RedirectUris:0"] = "https://app.akgaming.de/callback",
                ["OpenIddict:Applications:0:PostLogoutRedirectUris:0"] = "https://app.akgaming.de/logout-callback",
                ["OpenIddict:Applications:0:Scopes:0"] = "openid",
                ["OpenIddict:Applications:0:Scopes:1"] = "profile",
                ["OpenIddict:Applications:0:Scopes:2"] = "email",
                ["OpenIddict:Applications:0:Scopes:3"] = "roles",
                ["OpenIddict:Applications:0:Scopes:4"] = "offline_access",
                ["OpenIddict:Applications:0:Scopes:5"] = "management_api",
                ["OpenIddict:Applications:1:ClientId"] = "test-explicit-client",
                ["OpenIddict:Applications:1:DisplayName"] = "Test Explicit Client",
                ["OpenIddict:Applications:1:ConsentType"] = "explicit",
                ["OpenIddict:Applications:1:ClientType"] = "public",
                ["OpenIddict:Applications:1:RequirePkce"] = "true",
                ["OpenIddict:Applications:1:RedirectUris:0"] = "https://explicit.akgaming.de/callback",
                ["OpenIddict:Applications:1:PostLogoutRedirectUris:0"] = "https://explicit.akgaming.de/logout-callback",
                ["OpenIddict:Applications:1:Scopes:0"] = "openid",
                ["OpenIddict:Applications:1:Scopes:1"] = "profile",
                ["OpenIddict:Applications:1:Scopes:2"] = "email",
                ["OpenIddict:Applications:1:Scopes:3"] = "roles",
                ["OpenIddict:Applications:1:Scopes:4"] = "offline_access"
            };

            configurationBuilder.AddInMemoryCollection(overrides);
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDiscordOAuthService>();
            services.AddSingleton<IDiscordOAuthService, DiscordOAuthServiceStub>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
