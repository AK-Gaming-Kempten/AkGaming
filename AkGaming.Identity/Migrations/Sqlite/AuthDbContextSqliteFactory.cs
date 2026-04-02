using AkGaming.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Identity.Migrations.Sqlite;

public sealed class AuthDbContextSqliteFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__IdentityDb")
            ?? "Data Source=identity.db";

        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
        optionsBuilder.UseSqlite(
            connectionString,
            sqlite => sqlite.MigrationsAssembly(typeof(AuthDbContextSqliteFactory).Assembly.FullName));
        optionsBuilder.UseOpenIddict();

        return new AuthDbContext(optionsBuilder.Options);
    }
}
