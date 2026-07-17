using AkGaming.Management.Modules.Disbursements.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Management.Modules.Disbursements.Migrations.Sqlite;

public sealed class DisbursementsDbContextSqliteFactory : IDesignTimeDbContextFactory<DisbursementsDbContext>
{
    public DisbursementsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DisbursementsDbContext>().UseSqlite("Data Source=disbursements-design.db", database => database.MigrationsAssembly(typeof(DisbursementsDbContextSqliteFactory).Assembly.FullName)).Options;
        return new DisbursementsDbContext(options);
    }
}
