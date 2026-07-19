using AkGaming.Management.Modules.GeneralMeetings.Api.Realtime;
using AkGaming.Management.Modules.GeneralMeetings.Application;
using AkGaming.Management.Modules.GeneralMeetings.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Management.Modules.GeneralMeetings.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddGeneralMeetingsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddGeneralMeetingsApplication(); services.AddGeneralMeetingsInfrastructure(configuration); services.AddSignalR(); services.AddSingleton<MeetingPresenceTracker>();
        return services;
    }
    public static IEndpointRouteBuilder MapGeneralMeetingsHub(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<GeneralMeetingHub>("/hubs/general-meetings"); return endpoints;
    }
}
