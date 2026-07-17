using AkGaming.Management.Modules.Disbursements.Api.Controllers;
using AkGaming.Management.Modules.Disbursements.Application;
using AkGaming.Management.Modules.Disbursements.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Management.Modules.Disbursements.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddDisbursementsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers().AddApplicationPart(typeof(DisbursementsController).Assembly);
        services.AddDisbursementsApplication();
        services.AddDisbursementsInfrastructure(configuration);
        return services;
    }
}
