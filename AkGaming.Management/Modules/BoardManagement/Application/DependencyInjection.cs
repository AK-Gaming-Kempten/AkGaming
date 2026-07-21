using AkGaming.Management.Modules.BoardManagement.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Management.Modules.BoardManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddBoardManagementApplication(this IServiceCollection services)
    {
        services.AddScoped<IBoardMeetingService, BoardMeetingService>();
        return services;
    }
}
