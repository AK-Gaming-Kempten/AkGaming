using AkGaming.Management.Modules.InvoiceManagement.Application;
using AkGaming.Management.Modules.InvoiceManagement.Infrastructure;
using AkGaming.Management.Modules.InvoiceManagement.Api.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Management.Modules.InvoiceManagement.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddInvoiceManagementModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers().AddApplicationPart(typeof(InvoicesController).Assembly);
        services.AddInvoiceManagementApplication();
        services.AddInvoiceManagementInfrastructure(configuration);
        return services;
    }
}
