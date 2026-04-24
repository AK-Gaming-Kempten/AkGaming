using System.Net.Security;
using System.Security.Claims;
using AkGaming.Tournaments.Frontend.Api;
using AkGaming.Tournaments.Frontend.Authentication;
using AkGaming.Tournaments.Frontend.Components.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace AkGaming.Tournaments.Frontend.Startup;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRazorAndBlazor(this IServiceCollection services)
    {
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddHttpContextAccessor();
        services.AddScoped<OidcTokenStore>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, OidcTokenCircuitHandler>());

        return services;
    }

    public static IServiceCollection AddAuthenticationAndAuthorization(
        this IServiceCollection services,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        var oidcOptions = config.GetSection(OpenIdConnectClientOptions.SectionName).Get<OpenIdConnectClientOptions>() ?? new();
        var allowUntrustedLocalCertificates = env.IsDevelopment() && config.GetValue<bool>("Dev:AllowUntrustedLocalCertificates");

        services.Configure<OpenIdConnectClientOptions>(config.GetSection(OpenIdConnectClientOptions.SectionName));

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/authentication/login";
                options.LogoutPath = "/authentication/logout";
                options.AccessDeniedPath = "/account/access-denied";
                options.ClaimsIssuer = "AkGaming.Identity";
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = oidcOptions.Authority;
                options.ClientId = oidcOptions.ClientId;
                options.ClientSecret = oidcOptions.ClientSecret;
                options.CallbackPath = oidcOptions.CallbackPath;
                options.SignedOutCallbackPath = oidcOptions.SignedOutCallbackPath;
                options.RequireHttpsMetadata = oidcOptions.RequireHttpsMetadata;
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = false;
                options.MapInboundClaims = false;
                options.Scope.Clear();
                options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;

                if (allowUntrustedLocalCertificates)
                    options.BackchannelHttpHandler = CreateDevelopmentCertificateRelaxedHandler();

                foreach (var scope in oidcOptions.Scopes.Where(scope => !string.IsNullOrWhiteSpace(scope)).Distinct(StringComparer.Ordinal))
                    options.Scope.Add(scope);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "email",
                    RoleClaimType = "role"
                };

                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is not ClaimsIdentity identity)
                            return Task.CompletedTask;

                        var subject = context.Principal.FindFirst("sub")?.Value;
                        if (!string.IsNullOrWhiteSpace(subject) &&
                            !identity.HasClaim(claim => claim.Type == ClaimTypes.NameIdentifier))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subject));
                        }

                        var displayName = context.Principal.FindFirst("discord_username")?.Value
                                          ?? context.Principal.FindFirst("email")?.Value
                                          ?? context.Principal.FindFirst("name")?.Value;
                        if (!string.IsNullOrWhiteSpace(displayName) &&
                            !identity.HasClaim(claim => claim.Type == ClaimTypes.Name))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Name, displayName));
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection ConfigureForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    public static IServiceCollection AddTournamentMockData(this IServiceCollection services)
    {
        services.AddSingleton<MockTournamentCatalog>();
        return services;
    }

    public static IServiceCollection AddTournamentApiClients(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<TournamentApiOptions>(config.GetSection(TournamentApiOptions.SectionName));
        services.AddTransient<TournamentApiAuthorizationHandler>();

        services.AddTournamentApiClient<GamesApiClient>(config);
        services.AddTournamentApiClient<PlayerProfilesApiClient>(config);
        services.AddTournamentApiClient<TeamsApiClient>(config);
        services.AddTournamentApiClient<TournamentRegistrationsApiClient>(config);

        return services;
    }

    public static IServiceCollection AddDataProtectionForEnvironment(
        this IServiceCollection services,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName("AkGaming.Tournaments");

        var configuredKeyDirectory = config["DataProtection:KeyDirectory"];
        if (!string.IsNullOrWhiteSpace(configuredKeyDirectory))
        {
            Directory.CreateDirectory(configuredKeyDirectory);
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(configuredKeyDirectory));
            return services;
        }

        if (env.IsDevelopment())
            return services;

        var defaultKeyDirectory = ResolveDefaultKeyDirectory();
        Directory.CreateDirectory(defaultKeyDirectory);
        dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(defaultKeyDirectory));

        return services;
    }

    private static string ResolveDefaultKeyDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
            return Path.Combine(localAppData, "AkGaming", "Tournaments", "DataProtection-Keys");

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
            return Path.Combine(userProfile, ".akgaming", "tournaments", "keys");

        return Path.Combine(Path.GetTempPath(), "AkGaming.Tournaments", "DataProtection-Keys");
    }

    private static IHttpClientBuilder AddTournamentApiClient<TClient>(this IServiceCollection services, IConfiguration config)
        where TClient : class
    {
        var options = config.GetSection(TournamentApiOptions.SectionName).Get<TournamentApiOptions>() ?? new();
        var allowUntrustedLocalCertificates = config.GetValue<bool>("Dev:AllowUntrustedLocalCertificates");
        var builder = services
            .AddHttpClient<TClient>(client =>
            {
                client.BaseAddress = new Uri(options.BaseAddress);
            })
            .AddHttpMessageHandler<TournamentApiAuthorizationHandler>();

        if (allowUntrustedLocalCertificates)
            builder.ConfigurePrimaryHttpMessageHandler(CreateDevelopmentCertificateRelaxedHandler);

        return builder;
    }

    private static HttpClientHandler CreateDevelopmentCertificateRelaxedHandler()
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = static (request, _, _, errors) =>
            {
                if (errors == SslPolicyErrors.None)
                    return true;

                var host = request?.RequestUri?.Host;
                return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
                       || host == "127.0.0.1"
                       || host == "::1";
            }
        };
    }
}
