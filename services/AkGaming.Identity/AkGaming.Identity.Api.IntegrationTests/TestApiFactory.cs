using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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
                ["AllowedHosts"] = "*"
            };

            configurationBuilder.AddInMemoryCollection(overrides);
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
