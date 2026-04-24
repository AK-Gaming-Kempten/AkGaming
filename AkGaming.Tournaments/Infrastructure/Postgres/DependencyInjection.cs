using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Infrastructure.Postgres.Persistence;
using AkGaming.Tournaments.Infrastructure.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Tournaments.Infrastructure.Postgres;

public static class DependencyInjection
{
    public static IServiceCollection AddTournamentPostgresInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Postgres")
            ?? configuration["Persistence:PostgresConnectionString"]
            ?? "Host=localhost;Port=5432;Database=akgaming_tournaments;Username=postgres;Password=postgres";

        services.AddDbContext<TournamentDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IPlayerProfileRepository, PlayerProfileRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<ITournamentRepository, TournamentRepository>();
        services.AddScoped<ITournamentRegistrationRepository, TournamentRegistrationRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
