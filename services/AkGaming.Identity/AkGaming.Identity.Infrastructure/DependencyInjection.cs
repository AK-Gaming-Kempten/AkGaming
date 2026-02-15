using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Infrastructure.ExternalAuth;
using AkGaming.Identity.Infrastructure.Persistence;
using AkGaming.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<DiscordOptions>(configuration.GetSection(DiscordOptions.SectionName));

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
        services.AddDataProtection();
        services.AddHttpClient<IDiscordOAuthService, DiscordOAuthService>();
        services.AddSingleton<IDiscordStateService, DiscordStateService>();
        services.AddSingleton<IDiscordAuthSettings>(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiscordOptions>>().Value);

        return services;
    }
}
