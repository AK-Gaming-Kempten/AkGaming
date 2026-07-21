using AkGaming.Management.Modules.BoardManagement.Application;
using AkGaming.Management.Modules.BoardManagement.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Management.Modules.BoardManagement.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddBoardManagementModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBoardManagementApplication();
        services.AddBoardManagementInfrastructure(configuration);
        return services;
    }
}
