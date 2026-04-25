using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Tournaments.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTournamentApplication(this IServiceCollection services)
    {
        services.AddScoped<IGameCatalogService, GameCatalogService>();
        services.AddScoped<IMediaAssetService, MediaAssetService>();
        services.AddScoped<IPlayerProfileManagementService, PlayerProfileManagementService>();
        services.AddScoped<ITeamManagementService, TeamManagementService>();
        services.AddScoped<ITournamentLogoManagementService, TournamentLogoManagementService>();
        services.AddScoped<ITournamentRegistrationService, TournamentRegistrationService>();

        return services;
    }
}
