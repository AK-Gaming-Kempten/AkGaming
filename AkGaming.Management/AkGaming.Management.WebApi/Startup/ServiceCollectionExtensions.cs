using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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
    
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config) {
        var authority = config["Jwt:Authority"];
        var issuer = config["Jwt:Issuer"];
        var audience = config["Jwt:Audience"];
        var signingKey = config["Jwt:SigningKey"];

        services
            .AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", option => {
                if (!string.IsNullOrWhiteSpace(authority)) {
                    option.Authority = authority;
                    option.RequireHttpsMetadata = true;
                }

                option.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
                    ValidIssuer = issuer,
                    ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = !string.IsNullOrWhiteSpace(signingKey),
                    IssuerSigningKey = string.IsNullOrWhiteSpace(signingKey)
                        ? null
                        : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    NameClaimType = "email",
                    RoleClaimType = ClaimTypes.Role
                };
            });

        return services;
    }
    
    public static IServiceCollection AddAppAuthorization(this IServiceCollection services) {
        services.AddAuthorization(options => {
            options.AddPolicy("UserOnly", p => p.RequireRole("User", "Admin"));
            options.AddPolicy("MemberOnly", p => p.RequireRole("Member", "Admin"));
            options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
            options.AddPolicy("AdminOrSelfRouteUserId", p => p.RequireAssertion(ctx => {
                if (ctx.User.IsInRole("Admin")) return true;
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
}
