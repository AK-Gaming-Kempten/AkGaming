using AkGaming.Management.Modules.BoardManagement.Application.Interfaces;
using AkGaming.Management.Modules.BoardManagement.Infrastructure.Notifications;
using AkGaming.Management.Modules.BoardManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Management.Modules.BoardManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBoardManagementInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BoardNotificationOptions>(configuration.GetSection(BoardNotificationOptions.SectionName));
        var provider = configuration["Database:Provider"]?.Trim().ToLowerInvariant() ?? "postgres";
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<BoardManagementDbContext>(options =>
        {
            if (provider is "postgres" or "postgresql") options.UseNpgsql(connectionString ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required for Postgres."), database => database.MigrationsAssembly("AkGaming.Management.Modules.BoardManagement.Migrations.Postgres"));
            else if (provider == "sqlite") { var resolved = string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ? "Data Source=management.db" : connectionString; options.UseSqlite(resolved, database => database.MigrationsAssembly("AkGaming.Management.Modules.BoardManagement.Migrations.Sqlite")); options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)); }
            else throw new InvalidOperationException($"Unsupported database provider '{provider}'.");
        });
        services.AddScoped<IBoardMeetingRepository, EfBoardMeetingRepository>();
        services.AddScoped<IBoardNotificationOutbox, BoardNotificationOutbox>();
        services.AddScoped<BoardNotificationAccessTokenProvider>();
        services.AddHttpClient("BoardNotifications");
        services.AddHostedService<BoardNotificationOutboxDispatcher>();
        return services;
    }
}
