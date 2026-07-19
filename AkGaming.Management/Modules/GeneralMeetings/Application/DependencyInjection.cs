using AkGaming.Management.Modules.GeneralMeetings.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Management.Modules.GeneralMeetings.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddGeneralMeetingsApplication(this IServiceCollection services)
    {
        services.AddScoped<IGeneralMeetingService, GeneralMeetingService>();
        return services;
    }
}
