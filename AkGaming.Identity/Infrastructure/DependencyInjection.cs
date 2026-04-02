using AkGaming.Core.Common.Email;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Infrastructure.ExternalAuth;
using AkGaming.Identity.Infrastructure.OpenIddict;
using AkGaming.Identity.Infrastructure.Persistence;
using AkGaming.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AuthHardeningOptions>(configuration.GetSection(AuthHardeningOptions.SectionName));
        services.Configure<DiscordOptions>(configuration.GetSection(DiscordOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<AppUrlOptions>(configuration.GetSection(AppUrlOptions.SectionName));
        services.Configure<OpenIddictSeedOptions>(configuration.GetSection(OpenIddictSeedOptions.SectionName));
        services.Configure<OpenIddictCredentialOptions>(configuration.GetSection(OpenIddictCredentialOptions.SectionName));

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
                    options.ConfigureWarnings(warnings =>
                        warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported database provider '{provider}'. Supported values: Sqlite, Postgres.");
            }

            options.UseOpenIddict();
        });

        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
        services.AddDataProtection();
        services.AddHttpClient<IDiscordOAuthService, DiscordOAuthService>();
        services.AddSingleton<IDiscordStateService, DiscordStateService>();
        services.AddSingleton<IDiscordAuthSettings>(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiscordOptions>>().Value);
        services.AddSingleton<IAuthHardeningSettings>(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthHardeningOptions>>().Value);
        services.AddSingleton<IAppUrlSettings>(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppUrlOptions>>().Value);
        services.AddScoped<OpenIddictSeeder>();

        return services;
    }
}
