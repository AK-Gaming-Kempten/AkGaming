using Frontend.Blazor.ApiClients;
using Frontend.Blazor.Handlers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

namespace Frontend.Blazor.Startup;

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
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration config) {
        services.AddHttpContextAccessor();
        services.AddTransient<ApiAuthorizationHandler>();

        var apiBaseUrl = new Uri(config["Api:BaseUrl"]!);
        var identityApiBaseUrl = new Uri(config["IdentityApi:BaseUrl"] ?? config["Auth:BaseUrl"]!);

        services.AddHttpClient("ManagementApi", client => client.BaseAddress = apiBaseUrl)
            .AddHttpMessageHandler<ApiAuthorizationHandler>();

        services.AddHttpClient<MemberManagementApiClient>(client => client.BaseAddress = apiBaseUrl)
            .AddHttpMessageHandler<ApiAuthorizationHandler>();

        services.AddHttpClient<IdentityApiClient>(client => client.BaseAddress = identityApiBaseUrl)
            .AddHttpMessageHandler<ApiAuthorizationHandler>();

        return services;
    }

    public static IServiceCollection AddDataProtectionForEnvironment(this IServiceCollection services, IWebHostEnvironment env) {
        if (env.IsProduction()) {
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
                .SetApplicationName("ManagementTool");
        } else {
            services.AddDataProtection()
                .SetApplicationName("ManagementTool");
        }

        return services;
    }
}
