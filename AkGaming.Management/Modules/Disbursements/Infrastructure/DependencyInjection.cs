using AkGaming.Management.Modules.Disbursements.Application.Interfaces;
using AkGaming.Management.Modules.Disbursements.Infrastructure.Files;
using AkGaming.Management.Modules.Disbursements.Infrastructure.Persistence;
using AkGaming.Management.Modules.Disbursements.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Management.Modules.Disbursements.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDisbursementsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DisbursementNotificationOptions>(configuration.GetSection(DisbursementNotificationOptions.SectionName));
        var provider = configuration["Database:Provider"]?.Trim().ToLowerInvariant() ?? "postgres";
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<DisbursementsDbContext>(options =>
        {
            if (provider is "postgres" or "postgresql")
                options.UseNpgsql(connectionString ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required for Postgres."), database => database.MigrationsAssembly("AkGaming.Management.Modules.Disbursements.Migrations.Postgres"));
            else if (provider == "sqlite")
            {
                var resolved = string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ? "Data Source=management.db" : connectionString;
                options.UseSqlite(resolved, database => database.MigrationsAssembly("AkGaming.Management.Modules.Disbursements.Migrations.Sqlite"));
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            }
            else throw new InvalidOperationException($"Unsupported database provider '{provider}'.");
        });
        var storagePath = configuration["Disbursements:ReceiptStoragePath"];
        if (string.IsNullOrWhiteSpace(storagePath)) storagePath = Path.Combine(AppContext.BaseDirectory, "data", "disbursement-receipts");
        services.AddSingleton<IReceiptFileStorage>(new LocalReceiptFileStorage(Path.GetFullPath(storagePath)));
        services.AddScoped<IDisbursementRepository, EfDisbursementRepository>();
        services.AddScoped<IDisbursementNotificationOutbox, DisbursementNotificationOutbox>();
        services.AddScoped<NotificationAccessTokenProvider>();
        services.AddHttpClient("DisbursementNotifications");
        services.AddHostedService<DisbursementOutboxDispatcher>();
        return services;
    }
}
