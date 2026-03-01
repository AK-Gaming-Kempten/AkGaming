using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using AkGaming.Management.Modules.MemberManagement.Application.Services;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;

namespace AkGaming.Management.Modules.MemberManagement.Application;

public static class DependencyInjection {
    public static IServiceCollection AddMemberManagementApplication(this IServiceCollection services) {
        // Application services
        services.AddScoped<IMemberQueryService, MemberQueryService>();
        services.AddScoped<IMemberAuditLogService, MemberAuditLogService>();
        services.AddScoped<IMemberCreationService, MemberCreationService>();
        services.AddScoped<IMemberUpdateService, MemberUpdateService>();
        services.AddScoped<IMemberLinkingService, MemberLinkingService>();
        services.AddScoped<IMembershipUpdateService, MembershipUpdateService>();
        services.AddScoped<IMemberDeletionService, MemberDeletionService>();
        services.AddScoped<IMembershipApplicationService, MembershipApplicationService>();

        // Optionally register validators or behaviors
        // services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
