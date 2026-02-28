using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MemberManagement.Application;
using MemberManagement.Infrastructure;

namespace MemberManagement.Api;

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