using AkGaming.Management.Modules.GeneralMeetings.Application.Interfaces;
using AkGaming.Management.Modules.GeneralMeetings.Infrastructure.Persistence;
using AkGaming.Management.Modules.GeneralMeetings.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Management.Modules.GeneralMeetings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGeneralMeetingsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"]?.Trim().ToLowerInvariant() ?? "postgres";
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<GeneralMeetingsDbContext>(options =>
        {
            if (provider is "postgres" or "postgresql") options.UseNpgsql(connectionString ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required for Postgres."), database => database.MigrationsAssembly("AkGaming.Management.Modules.GeneralMeetings.Migrations.Postgres"));
            else if (provider == "sqlite") { var resolved = string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ? "Data Source=management.db" : connectionString; options.UseSqlite(resolved, database => database.MigrationsAssembly("AkGaming.Management.Modules.GeneralMeetings.Migrations.Sqlite")); options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)); }
            else throw new InvalidOperationException($"Unsupported database provider '{provider}'.");
        });
        services.AddScoped<IGeneralMeetingRepository, EfGeneralMeetingRepository>();
        var dataProtection = services.AddDataProtection().SetApplicationName("AkGaming.Management.GeneralMeetings");
        var keysPath = configuration["GeneralMeetings:DataProtectionKeysPath"];
        if (!string.IsNullOrWhiteSpace(keysPath)) dataProtection.PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(keysPath)));
        services.AddSingleton<IBallotCredentialProtector, DataProtectionBallotCredentialProtector>();
        return services;
    }
}
