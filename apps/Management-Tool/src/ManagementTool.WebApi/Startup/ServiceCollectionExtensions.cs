using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ManagementTool.WebApi.Startup;

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
        services
            .AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", option => {
                option.Authority = config["Jwt:Authority"];
                option.Audience = config["Jwt:Audience"];
                option.RequireHttpsMetadata = true;
                option.TokenValidationParameters = new TokenValidationParameters {
                    NameClaimType = "preferred_username",
                    RoleClaimType = ClaimTypes.Role
                };
                option.Events = new JwtBearerEvents {
                    OnTokenValidated = context => {
                        var identity = (ClaimsIdentity)context.Principal!.Identity!;
                        var realmRoles = context.Principal.FindAll("realm_access.roles");
                        foreach (var r in realmRoles)
                            identity.AddClaim(new Claim(ClaimTypes.Role, r.Value));

                        var resourceRoles = context.Principal.FindAll("resource_access.managementtool-api.roles");
                        foreach (var r in resourceRoles)
                            identity.AddClaim(new Claim(ClaimTypes.Role, r.Value));

                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }
    
    public static IServiceCollection AddAppAuthorization(this IServiceCollection services) {
        services.AddAuthorization(options => {
            options.AddPolicy("UserOnly", p => p.RequireRole("User", "Admin"));
            options.AddPolicy("MemberOnly", p => p.RequireRole("Member", "Admin"));
            options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
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
