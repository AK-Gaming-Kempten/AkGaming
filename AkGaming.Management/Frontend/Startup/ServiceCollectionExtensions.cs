using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Frontend.Handlers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net.Security;

namespace AkGaming.Management.Frontend.Startup;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddRazorAndBlazor(this IServiceCollection services) {
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        services.AddRazorPages();
        services.AddServerSideBlazor();

        return services;
    }

    public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services, IConfiguration config) {
        services.AddAuthentication(options => {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "Cookies";
            })
            .AddCookie("Cookies", options => {
                options.LoginPath = "/authentication/login";
                options.LogoutPath = "/authentication/logout";
                options.AccessDeniedPath = "/account/accessdenied";
                options.ClaimsIssuer = "AkGaming.Identity";
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
        services.AddHttpContextAccessor();
        services.AddTransient<ApiAuthorizationHandler>();

        var apiBaseUrl = new Uri(config["Api:BaseUrl"]!);
        var identityApiBaseUrl = new Uri(config["IdentityApi:BaseUrl"] ?? config["Auth:BaseUrl"]!);
        var allowUntrustedLocalCertificates = env.IsDevelopment() && config.GetValue<bool>("Dev:AllowUntrustedLocalCertificates");

        var managementApiClient = services
            .AddHttpClient("ManagementApi", client => client.BaseAddress = apiBaseUrl)
            .AddHttpMessageHandler<ApiAuthorizationHandler>();
        ConfigureDevelopmentCertificateRelaxation(managementApiClient, allowUntrustedLocalCertificates);

        var memberManagementApiClient = services
            .AddHttpClient<MemberManagementApiClient>(client => client.BaseAddress = apiBaseUrl)
            .AddHttpMessageHandler<ApiAuthorizationHandler>();
        ConfigureDevelopmentCertificateRelaxation(memberManagementApiClient, allowUntrustedLocalCertificates);

        var identityApiClient = services
            .AddHttpClient<IdentityApiClient>(client => client.BaseAddress = identityApiBaseUrl)
            .AddHttpMessageHandler<ApiAuthorizationHandler>();
        ConfigureDevelopmentCertificateRelaxation(identityApiClient, allowUntrustedLocalCertificates);

        return services;
    }

    private static void ConfigureDevelopmentCertificateRelaxation(IHttpClientBuilder clientBuilder, bool allowUntrustedLocalCertificates) {
        if (!allowUntrustedLocalCertificates)
            return;

        clientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler {
            ServerCertificateCustomValidationCallback = static (request, _, _, errors) => {
                if (errors == SslPolicyErrors.None)
                    return true;

                var host = request?.RequestUri?.Host;
                return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
                       || host == "127.0.0.1"
                       || host == "::1";
            }
        });
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
}
