using AkGaming.Management.Modules.GeneralMeetings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Management.Modules.GeneralMeetings.Migrations.Postgres;

public sealed class GeneralMeetingsDbContextPostgresFactory : IDesignTimeDbContextFactory<GeneralMeetingsDbContext>
{
    public GeneralMeetingsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=general_meetings_design;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<GeneralMeetingsDbContext>()
            .UseNpgsql(
                connectionString,
                database => database.MigrationsAssembly(typeof(GeneralMeetingsDbContextPostgresFactory).Assembly.FullName))
            .Options;
        return new GeneralMeetingsDbContext(options);
    }
}
