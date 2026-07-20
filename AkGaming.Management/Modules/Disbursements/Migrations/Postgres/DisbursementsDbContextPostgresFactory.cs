using AkGaming.Management.Modules.Disbursements.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Management.Modules.Disbursements.Migrations.Postgres;

public sealed class DisbursementsDbContextPostgresFactory : IDesignTimeDbContextFactory<DisbursementsDbContext>
{
    public DisbursementsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=disbursements_design;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<DisbursementsDbContext>()
            .UseNpgsql(
                connectionString,
                database => database.MigrationsAssembly(typeof(DisbursementsDbContextPostgresFactory).Assembly.FullName))
            .Options;
        return new DisbursementsDbContext(options);
    }
}
