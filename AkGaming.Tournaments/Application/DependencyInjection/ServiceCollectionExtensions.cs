using AkGaming.Tournaments.Application.Abstractions;
using AkGaming.Tournaments.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Tournaments.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTournamentApplication(this IServiceCollection services)
    {
        services.AddScoped<IGameCatalogService, GameCatalogService>();
        services.AddScoped<IPlayerProfileManagementService, PlayerProfileManagementService>();
        services.AddScoped<ITeamManagementService, TeamManagementService>();
        services.AddScoped<ITournamentRegistrationService, TournamentRegistrationService>();

        return services;
    }
}
