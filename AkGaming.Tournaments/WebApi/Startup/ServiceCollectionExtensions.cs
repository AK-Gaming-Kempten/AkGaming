using System.Net.Security;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;

namespace AkGaming.Tournaments.WebApi.Startup;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTournamentJsonAndControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return services;
    }

    public static IServiceCollection AddTournamentSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi();
        services.AddSwaggerGen();
        return services;
    }

    public static IServiceCollection AddOpenIddictAuthentication(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
    {
        var validationOptions =
            config.GetSection(OpenIddictValidationOptions.SectionName).Get<OpenIddictValidationOptions>() ?? new();
        var allowUntrustedLocalCertificates =
            env.IsDevelopment() && config.GetValue<bool>("Dev:AllowUntrustedLocalCertificates");

        services.Configure<OpenIddictValidationOptions>(config.GetSection(OpenIddictValidationOptions.SectionName));

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        services.AddOpenIddict()
            .AddValidation(options =>
            {
                if (string.IsNullOrWhiteSpace(validationOptions.Issuer))
                    throw new InvalidOperationException("OpenIddictValidation:Issuer is required.");

                options.SetIssuer(new Uri(validationOptions.Issuer, UriKind.Absolute));
                options.UseSystemNetHttp(builder =>
                {
                    if (!allowUntrustedLocalCertificates)
                        return;

                    builder.ConfigureHttpClientHandler(handler =>
                    {
                        handler.ServerCertificateCustomValidationCallback = static (request, _, _, errors) =>
                        {
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

    public static IServiceCollection AddTournamentAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = BuildTournamentApiPolicy().Build();
            options.AddPolicy("AdminOnly", policy =>
                BuildTournamentApiPolicy(policy).RequireAssertion(context => HasRole(context.User, "Admin")));
            options.AddPolicy("AdminOrSelfRouteUserId", policy =>
                BuildTournamentApiPolicy(policy).RequireAssertion(context =>
                {
                    if (HasRole(context.User, "Admin"))
                        return true;

                    if (context.Resource is not HttpContext http)
                        return false;

                    var routeUserId = http.Request.RouteValues.TryGetValue("userId", out var value) ? value?.ToString() : null;
                    var currentUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");

                    return !string.IsNullOrWhiteSpace(routeUserId)
                           && !string.IsNullOrWhiteSpace(currentUserId)
                           && string.Equals(routeUserId, currentUserId, StringComparison.OrdinalIgnoreCase);
                }));
        });

        return services;
    }

    private static AuthorizationPolicyBuilder BuildTournamentApiPolicy(AuthorizationPolicyBuilder? builder = null)
    {
        builder ??= new AuthorizationPolicyBuilder();
        builder.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        builder.RequireAuthenticatedUser();
        builder.RequireAssertion(context => HasScope(context.User, "tournaments_api"));
        return builder;
    }

    private static bool HasRole(ClaimsPrincipal principal, string role)
    {
        return principal.IsInRole(role)
               || principal.Claims.Any(claim =>
                   (claim.Type == "role" || claim.Type == ClaimTypes.Role)
                   && string.Equals(claim.Value, role, StringComparison.Ordinal));
    }

    private static bool HasScope(ClaimsPrincipal principal, string scope)
    {
        return principal.Claims
            .Where(claim => claim.Type == "scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(value => string.Equals(value, scope, StringComparison.Ordinal));
    }
}
