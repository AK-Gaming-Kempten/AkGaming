using AkGaming.InvoiceGenerator.Core.Rendering;
using AkGaming.Management.Modules.InvoiceManagement.Application.Services;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Management.Modules.InvoiceManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddInvoiceManagementApplication(this IServiceCollection services)
    {
        services.AddScoped<IInvoiceManagementService, InvoiceManagementService>();
        services.AddSingleton<IInvoiceHtmlRenderer, InvoiceHtmlRenderer>();
        services.AddSingleton<IInvoicePdfRenderer, InvoicePdfRenderer>();
        return services;
    }
}
