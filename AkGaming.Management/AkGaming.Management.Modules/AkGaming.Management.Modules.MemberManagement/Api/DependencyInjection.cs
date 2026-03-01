using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AkGaming.Management.Modules.MemberManagement.Application;
using AkGaming.Management.Modules.MemberManagement.Infrastructure;

namespace AkGaming.Management.Modules.MemberManagement.Api;

public static class DependencyInjection {
    public static IServiceCollection AddMemberManagementModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemberManagementApplication();
        services.AddMemberManagementInfrastructure(configuration);

        return services;
    }
}