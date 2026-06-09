using AkGaming.Management.Modules.InvoiceManagement.Application.Interfaces;
using AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence;
using AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Management.Modules.InvoiceManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInvoiceManagementInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"]?.Trim().ToLowerInvariant() ?? "postgres";
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<InvoiceManagementDbContext>(options =>
        {
            switch (provider)
            {
                case "postgres":
                case "postgresql":
                    options.UseNpgsql(
                        connectionString ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required for Postgres."),
                        npgsql => npgsql.MigrationsAssembly(InvoiceManagementDbContextMigrations.PostgresAssembly));
                    break;
                case "sqlite":
                    options.UseSqlite(
                        ResolveSqliteConnectionString(connectionString),
                        sqlite => sqlite.MigrationsAssembly(InvoiceManagementDbContextMigrations.SqliteAssembly));
                    options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported database provider '{provider}'. Supported values: Sqlite, Postgres.");
            }
        });

        services.AddScoped<IInvoiceRepository, EfInvoiceRepository>();
        return services;
    }

    private static string ResolveSqliteConnectionString(string? configuredConnectionString)
    {
        if (string.IsNullOrWhiteSpace(configuredConnectionString)
            || configuredConnectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
            return "Data Source=management.db";

        return configuredConnectionString;
    }
}
