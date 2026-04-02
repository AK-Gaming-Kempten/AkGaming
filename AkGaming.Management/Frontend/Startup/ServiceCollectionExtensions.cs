using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Frontend.Authentication;
using AkGaming.Management.Frontend.Handlers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Net.Security;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AkGaming.Management.Frontend.Startup;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddRazorAndBlazor(this IServiceCollection services) {
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddHttpContextAccessor();
        services.AddScoped<FrontendSessionCoordinator>();
        services.AddScoped<OidcTokenStore>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, OidcTokenCircuitHandler>());

        return services;
    }

    public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env) {
        var oidcOptions = config.GetSection(OpenIdConnectClientOptions.SectionName).Get<OpenIdConnectClientOptions>() ?? new();
        var allowUntrustedLocalCertificates = env.IsDevelopment() && config.GetValue<bool>("Dev:AllowUntrustedLocalCertificates");
        services.Configure<OpenIdConnectClientOptions>(config.GetSection(OpenIdConnectClientOptions.SectionName));

        services.AddAuthentication(options => {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => {
                options.LoginPath = "/authentication/login";
                options.LogoutPath = "/authentication/logout";
                options.AccessDeniedPath = "/account/accessdenied";
                options.ClaimsIssuer = "AkGaming.Identity";
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options => {
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
                if (allowUntrustedLocalCertificates)
                    options.BackchannelHttpHandler = CreateDevelopmentCertificateRelaxedHandler();
                options.Scope.Clear();

                foreach (var scope in oidcOptions.Scopes.Where(scope => !string.IsNullOrWhiteSpace(scope)).Distinct(StringComparer.Ordinal))
                    options.Scope.Add(scope);

                options.TokenValidationParameters = new TokenValidationParameters {
                    NameClaimType = "email",
                    RoleClaimType = "role"
                };
                options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
                options.Events = new OpenIdConnectEvents {
                    OnTokenValidated = context => {
                        if (context.Principal?.Identity is not ClaimsIdentity identity)
                            return Task.CompletedTask;

                        var subject = context.Principal.FindFirst("sub")?.Value;
                        if (!string.IsNullOrWhiteSpace(subject) && !identity.HasClaim(claim => claim.Type == ClaimTypes.NameIdentifier))
                            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subject));

                        var displayName = context.Principal.FindFirst("email")?.Value ?? context.Principal.FindFirst("name")?.Value;
                        if (!string.IsNullOrWhiteSpace(displayName) && !identity.HasClaim(claim => claim.Type == ClaimTypes.Name))
                            identity.AddClaim(new Claim(ClaimTypes.Name, displayName));

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection ConfigureForwardedHeaders(this IServiceCollection services) {
        services.Configure<ForwardedHeadersOptions>(options => {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env) {
        services.AddTransient<ApiAuthorizationHandler>();

        var apiBaseUrl = new Uri(config["Api:BaseUrl"]!);
        var identityApiBaseUrl = new Uri(config["IdentityApi:BaseUrl"] ?? config["OpenIdConnect:Authority"]!);
        var allowUntrustedLocalCertificates = env.IsDevelopment() && config.GetValue<bool>("Dev:AllowUntrustedLocalCertificates");

        var managementApiClient = services
            .AddHttpClient("ManagementApi", client => client.BaseAddress = apiBaseUrl)
            .AddApplicationScopeHandler()
            .AddHttpMessageHandler<ApiAuthorizationHandler>();
        ConfigureDevelopmentCertificateRelaxation(managementApiClient, allowUntrustedLocalCertificates);

        var identityApiClient = services
            .AddHttpClient("IdentityApi", client => client.BaseAddress = identityApiBaseUrl)
            .AddApplicationScopeHandler()
            .AddHttpMessageHandler<ApiAuthorizationHandler>();
        ConfigureDevelopmentCertificateRelaxation(identityApiClient, allowUntrustedLocalCertificates);

        var oidcBackchannelClient = services.AddHttpClient("OidcBackchannel");
        ConfigureDevelopmentCertificateRelaxation(oidcBackchannelClient, allowUntrustedLocalCertificates);

        services.AddScoped<MemberManagementApiClient>(sp =>
            new MemberManagementApiClient(sp.GetRequiredKeyedService<HttpClient>("ManagementApi")));

        services.AddScoped<IdentityApiClient>(sp =>
            new IdentityApiClient(
                sp.GetRequiredKeyedService<HttpClient>("IdentityApi"),
                sp.GetRequiredService<IConfiguration>()));

        return services;
    }

    private static void ConfigureDevelopmentCertificateRelaxation(IHttpClientBuilder clientBuilder, bool allowUntrustedLocalCertificates) {
        if (!allowUntrustedLocalCertificates)
            return;

        clientBuilder.ConfigurePrimaryHttpMessageHandler(CreateDevelopmentCertificateRelaxedHandler);
    }

    public static IServiceCollection AddDataProtectionForEnvironment(this IServiceCollection services, IWebHostEnvironment env) {
        if (env.IsProduction()) {
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
                .SetApplicationName("AkGaming.Management");
        } else {
            services.AddDataProtection()
                .SetApplicationName("AkGaming.Management");
        }

        return services;
    }

    private static HttpClientHandler CreateDevelopmentCertificateRelaxedHandler() {
        return new HttpClientHandler {
            ServerCertificateCustomValidationCallback = static (request, _, _, errors) => {
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
