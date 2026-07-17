using AkGaming.Management.Modules.Disbursements.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Management.Modules.Disbursements.Migrations.Postgres;

public sealed class DisbursementsDbContextPostgresFactory : IDesignTimeDbContextFactory<DisbursementsDbContext>
{
    public DisbursementsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DisbursementsDbContext>().UseNpgsql("Host=localhost;Database=disbursements_design;Username=postgres;Password=postgres", database => database.MigrationsAssembly(typeof(DisbursementsDbContextPostgresFactory).Assembly.FullName)).Options;
        return new DisbursementsDbContext(options);
    }
}
