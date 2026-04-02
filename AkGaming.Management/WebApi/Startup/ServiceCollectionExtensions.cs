using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;
using System.Net.Security;

namespace AkGaming.Management.WebApi.Startup;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddJsonAndControllers(this IServiceCollection services) {
        services.AddControllers()
            .AddJsonOptions(o =>
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.ConfigureHttpJsonOptions(o =>
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        return services;
    }
    
    public static IServiceCollection AddOpenIddictAuthentication(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env) {
        var validationOptions = config.GetSection(OpenIddictValidationOptions.SectionName).Get<OpenIddictValidationOptions>() ?? new();
        var allowUntrustedLocalCertificates = env.IsDevelopment() && config.GetValue<bool>("Dev:AllowUntrustedLocalCertificates");
        services.Configure<OpenIddictValidationOptions>(config.GetSection(OpenIddictValidationOptions.SectionName));

        services.AddAuthentication(options => {
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        services.AddOpenIddict()
            .AddValidation(options => {
                if (string.IsNullOrWhiteSpace(validationOptions.Issuer))
                    throw new InvalidOperationException("OpenIddictValidation:Issuer is required.");

                options.SetIssuer(new Uri(validationOptions.Issuer, UriKind.Absolute));
                options.UseSystemNetHttp(builder => {
                    if (!allowUntrustedLocalCertificates)
                        return;

                    builder.ConfigureHttpClientHandler(handler => {
                        handler.ServerCertificateCustomValidationCallback = static (request, _, _, errors) => {
                            if (errors == SslPolicyErrors.None)
                                return true;

                            var host = request?.RequestUri?.Host;
                            return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
                                   || host == "127.0.0.1"
                                   || host == "::1";
                        };
                    });
                });
                options.UseAspNetCore();
            });

        return services;
    }
    
    public static IServiceCollection AddAppAuthorization(this IServiceCollection services) {
        services.AddAuthorization(options => {
            options.DefaultPolicy = BuildManagementApiPolicy().Build();
            options.AddPolicy("UserOnly", p => BuildManagementApiPolicy(p).RequireAssertion(ctx => HasAnyRole(ctx.User, "User", "Admin")));
            options.AddPolicy("MemberOnly", p => BuildManagementApiPolicy(p).RequireAssertion(ctx => HasAnyRole(ctx.User, "Member", "Admin")));
            options.AddPolicy("AdminOnly", p => BuildManagementApiPolicy(p).RequireAssertion(ctx => HasRole(ctx.User, "Admin")));
            options.AddPolicy("AdminOrSelfRouteUserId", p => BuildManagementApiPolicy(p).RequireAssertion(ctx => {
                if (HasRole(ctx.User, "Admin")) return true;
                if (ctx.Resource is not HttpContext http) return false;

                var routeVal = http.Request.RouteValues.TryGetValue("userId", out var v) ? v?.ToString() : null;
                if (!Guid.TryParse(routeVal, out var routeUserId)) return false;

                var claim = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ctx.User.FindFirstValue("sub");
                return Guid.TryParse(claim, out var currentUserId) && currentUserId == routeUserId;
            }));
        });
        return services;
    }
    
    public static IServiceCollection AddAppSwagger(this IServiceCollection services) {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options => {
            options.SchemaGeneratorOptions = new() {
                UseInlineDefinitionsForEnums = true
            };
        });
        return services;
    }

    private static AuthorizationPolicyBuilder BuildManagementApiPolicy(AuthorizationPolicyBuilder? builder = null) {
        builder ??= new AuthorizationPolicyBuilder();
        builder.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        builder.RequireAuthenticatedUser();
        builder.RequireAssertion(context => HasScope(context.User, "management_api"));
        return builder;
    }

    private static bool HasAnyRole(ClaimsPrincipal principal, params string[] roles) {
        return roles.Any(role => HasRole(principal, role));
    }

    private static bool HasRole(ClaimsPrincipal principal, string role) {
        return principal.IsInRole(role)
               || principal.Claims.Any(claim =>
                   (claim.Type == "role" || claim.Type == ClaimTypes.Role)
                   && string.Equals(claim.Value, role, StringComparison.Ordinal));
    }

    private static bool HasScope(ClaimsPrincipal principal, string scope) {
        return principal.Claims
            .Where(claim => claim.Type == "scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(value => string.Equals(value, scope, StringComparison.Ordinal));
    }
}
