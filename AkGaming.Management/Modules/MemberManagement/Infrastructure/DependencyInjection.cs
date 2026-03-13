using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AkGaming.Core.Common.Email;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;
using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence.Repositories;

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure;

public static class DependencyInjection {
    public static IServiceCollection AddMemberManagementInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"]?.Trim().ToLowerInvariant() ?? "postgres";
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<MemberManagementDbContext>(options =>
        {
            switch (provider)
            {
                case "postgres":
                case "postgresql":
                    options.UseNpgsql(
                        connectionString ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required for Postgres."),
                        npgsql => npgsql.MigrationsAssembly(typeof(MemberManagementDbContext).Assembly.FullName));
                    break;
                case "sqlite":
                    options.UseSqlite(ResolveSqliteConnectionString(connectionString));
                    options.ConfigureWarnings(warnings =>
                        warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported database provider '{provider}'. Supported values: Sqlite, Postgres.");
            }
        });

        services.AddScoped<IMemberRepository, EfMemberRepository>();
        services.AddScoped<IMemberAuditLogRepository, EfMemberAuditLogRepository>();
        services.AddScoped<IMembershipApplicationRequestRepository, EfMembershipApplicationRequestRepository>();
        services.AddScoped<IMemberLinkingRequestRepository, EfMemberLinkingRequestRepository>();
        services.AddScoped<IMemberAuditLogWriter, EfMemberAuditLogWriter>();
        services.AddScoped<IMembershipPaymentPeriodRepository, EfMembershipPaymentPeriodRepository>();
        services.AddScoped<IMembershipDueRepository, EfMembershipDueRepository>();
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        return services;
    }

    private static string ResolveSqliteConnectionString(string? configuredConnectionString)
    {
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
            return "Data Source=management.db";

        // Common local pitfall: user-secrets/environment still provide a Postgres string while provider is Sqlite.
        if (configuredConnectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
            return "Data Source=management.db";

        return configuredConnectionString;
    }
}
