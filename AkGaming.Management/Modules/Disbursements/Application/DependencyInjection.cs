using AkGaming.Management.Modules.Disbursements.Application.Services;
using AkGaming.Management.Modules.Disbursements.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Management.Modules.Disbursements.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddDisbursementsApplication(this IServiceCollection services)
    {
        services.AddScoped<IDisbursementService, DisbursementService>();
        return services;
    }
}
