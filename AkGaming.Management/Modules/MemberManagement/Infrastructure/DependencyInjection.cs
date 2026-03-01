using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;
using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence.Repositories;

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure;

public static class DependencyInjection {
    public static IServiceCollection AddMemberManagementInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<MemberManagementDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(MemberManagementDbContext).Assembly.FullName)));

        services.AddScoped<IMemberRepository, EfMemberRepository>();
        services.AddScoped<IMemberAuditLogRepository, EfMemberAuditLogRepository>();
        services.AddScoped<IMembershipApplicationRequestRepository, EfMembershipApplicationRequestRepository>();
        services.AddScoped<IMemberLinkingRequestRepository, EfMemberLinkingRequestRepository>();
        services.AddScoped<IMemberAuditLogWriter, EfMemberAuditLogWriter>();

        return services;
    }
}
