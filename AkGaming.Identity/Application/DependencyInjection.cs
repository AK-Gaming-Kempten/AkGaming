using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
