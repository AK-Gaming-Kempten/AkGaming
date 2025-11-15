using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MemberManagement.Application.Interfaces;
using MemberManagement.Infrastructure.Persistence;
using MemberManagement.Infrastructure.Persistence.Repositories;

namespace MemberManagement.Infrastructure;

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
        services.AddScoped<IMembershipApplicationRequestRepository, EfMembershipApplicationRequestRepository>();
        services.AddScoped<IMemberLinkingRequestRepository, EfMemberLinkingRequestRepository>();

        return services;
    }
}