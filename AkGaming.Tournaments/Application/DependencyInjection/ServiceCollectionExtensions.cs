using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.Application.RegistrationRules;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Tournaments.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTournamentApplication(this IServiceCollection services)
    {
        services.AddScoped<IGameCatalogService, GameCatalogService>();
        services.AddSingleton<IGameRankSystemRegistry, GameRankSystemRegistry>();
        services.AddScoped<IMediaAssetService, MediaAssetService>();
        services.AddScoped<IPlayerProfileManagementService, PlayerProfileManagementService>();
        services.AddScoped<ITeamManagementService, TeamManagementService>();
        services.AddScoped<ITournamentCatalogService, TournamentCatalogService>();
        services.AddScoped<ITournamentAdministrationService, TournamentAdministrationService>();
        services.AddScoped<ITournamentContentManagementService, TournamentContentManagementService>();
        services.AddScoped<ITournamentLogoManagementService, TournamentLogoManagementService>();
        services.AddScoped<ITournamentRegistrationRuleManagementService, TournamentRegistrationRuleManagementService>();
        services.AddScoped<ITournamentRegistrationService, TournamentRegistrationService>();

        return services;
    }
}
