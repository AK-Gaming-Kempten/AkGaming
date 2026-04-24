using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Infrastructure.Sqlite.Persistence;
using AkGaming.Tournaments.Infrastructure.Sqlite.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Tournaments.Infrastructure.Sqlite;

public static class DependencyInjection
{
    public static IServiceCollection AddTournamentSqliteInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Sqlite")
            ?? configuration["Persistence:SqliteConnectionString"]
            ?? "Data Source=akgaming-tournaments.db";

        services.AddDbContext<TournamentDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IPlayerProfileRepository, PlayerProfileRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<ITournamentRepository, TournamentRepository>();
        services.AddScoped<ITournamentRegistrationRepository, TournamentRegistrationRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
