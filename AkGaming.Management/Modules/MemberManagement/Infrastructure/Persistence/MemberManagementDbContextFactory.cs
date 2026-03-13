using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;

public class MemberManagementDbContextFactory : IDesignTimeDbContextFactory<MemberManagementDbContext> {
    public MemberManagementDbContext CreateDbContext(string[] args) {
        var provider = Environment.GetEnvironmentVariable("Database__Provider")?.Trim().ToLowerInvariant() ?? "postgres";
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<MemberManagementDbContext>();
        switch (provider) {
            case "postgres":
            case "postgresql":
                optionsBuilder.UseNpgsql(connectionString
                                         ?? throw new InvalidOperationException("Missing env var ConnectionStrings__DefaultConnection for Postgres."));
                break;
            case "sqlite":
                optionsBuilder.UseSqlite(ResolveSqliteConnectionString(connectionString));
                break;
            default:
                throw new InvalidOperationException($"Unsupported Database__Provider '{provider}'. Supported values: Sqlite, Postgres.");
        }

        return new MemberManagementDbContext(optionsBuilder.Options);
    }

    private static string ResolveSqliteConnectionString(string? configuredConnectionString) {
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
            return "Data Source=management.db";

        if (configuredConnectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
            return "Data Source=management.db";

        return configuredConnectionString;
    }
}
