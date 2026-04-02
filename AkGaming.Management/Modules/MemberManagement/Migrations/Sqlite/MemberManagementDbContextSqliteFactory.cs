using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Management.Modules.MemberManagement.Migrations.Sqlite;

public sealed class MemberManagementDbContextSqliteFactory : IDesignTimeDbContextFactory<MemberManagementDbContext>
{
    public MemberManagementDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Data Source=management.db";

        var optionsBuilder = new DbContextOptionsBuilder<MemberManagementDbContext>();
        optionsBuilder.UseSqlite(
            connectionString,
            sqlite => sqlite.MigrationsAssembly(typeof(MemberManagementDbContextSqliteFactory).Assembly.FullName));

        return new MemberManagementDbContext(optionsBuilder.Options);
    }
}
