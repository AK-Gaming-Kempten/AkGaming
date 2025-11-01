using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using MemberManagement.Application.Services;
using MemberManagement.Contracts.Services;

namespace MemberManagement.Application;

public static class DependencyInjection {
    public static IServiceCollection AddMemberManagementApplication(this IServiceCollection services) {
        // Application services
        services.AddScoped<IMemberQueryService, MemberQueryService>();
        services.AddScoped<IMemberCreationService, MemberCreationService>();
        services.AddScoped<IMemberUpdateService, MemberUpdateService>();
        services.AddScoped<IMemberLinkingService, MemberLinkingService>();
        services.AddScoped<IMembershipUpdateService, MembershipUpdateService>();
        services.AddScoped<IMemberDeletionService, MemberDeletionService>();

        // Optionally register validators or behaviors
        // services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}