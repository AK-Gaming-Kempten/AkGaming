using Frontend.Blazor.ApiClients;
using Frontend.Blazor.Handlers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
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
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("Cookies")
            .AddOpenIdConnect("oidc", options => {
                options.Authority = config["Oidc:Authority"];
                options.ClientId = config["Oidc:ClientId"];
                options.ClientSecret = config["Oidc:ClientSecret"];
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;

                options.CallbackPath = config["Oidc:CallbackPath"];
                options.SignedOutCallbackPath = config["Oidc:SignedOutCallbackPath"];

                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.Scope.Add("offline_access");
                options.Scope.Add("managementtool-api-dedicated");
            });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection ConfigureForwardedHeaders(this IServiceCollection services) {
        services.Configure<ForwardedHeadersOptions>(options => {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration config) {
        services.AddHttpContextAccessor();
        services.AddTransient<ApiAuthorizationHandler>();

        var apiBaseUrl = new Uri(config["Api:BaseUrl"]!);

        services.AddHttpClient("ManagementApi", client => client.BaseAddress = apiBaseUrl)
            .AddHttpMessageHandler<ApiAuthorizationHandler>();

        services.AddHttpClient<MemberManagementApiClient>(client => client.BaseAddress = apiBaseUrl)
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
