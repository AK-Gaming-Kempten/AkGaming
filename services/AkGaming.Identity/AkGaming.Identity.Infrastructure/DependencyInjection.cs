using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Infrastructure.Persistence;
using AkGaming.Identity.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var provider = configuration["Database:Provider"]?.Trim().ToLowerInvariant() ?? "sqlite";
        var connectionString = configuration.GetConnectionString("IdentityDb");

        services.AddDbContext<AuthDbContext>(options =>
        {
            switch (provider)
            {
                case "postgres":
                case "postgresql":
                    options.UseNpgsql(connectionString ?? throw new InvalidOperationException("ConnectionStrings:IdentityDb is required for Postgres."));
                    break;
                case "sqlite":
                    options.UseSqlite(connectionString ?? "Data Source=identity.db");
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported database provider '{provider}'. Supported values: Sqlite, Postgres.");
            }
        });

        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();

        return services;
    }
}
