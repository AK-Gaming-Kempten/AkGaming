using AkGaming.Management.Modules.GeneralMeetings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Management.Modules.GeneralMeetings.Migrations.Postgres;
public sealed class GeneralMeetingsDbContextPostgresFactory : IDesignTimeDbContextFactory<GeneralMeetingsDbContext>
{
    public GeneralMeetingsDbContext CreateDbContext(string[] args) { var builder = new DbContextOptionsBuilder<GeneralMeetingsDbContext>(); builder.UseNpgsql("Host=localhost;Database=akgaming;Username=postgres;Password=postgres", x => x.MigrationsAssembly(typeof(GeneralMeetingsDbContextPostgresFactory).Assembly.FullName)); return new GeneralMeetingsDbContext(builder.Options); }
}
