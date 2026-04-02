using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Management.Modules.MemberManagement.Migrations.Postgres;

public sealed class MemberManagementDbContextPostgresFactory : IDesignTimeDbContextFactory<MemberManagementDbContext>
{
    public MemberManagementDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=akgaming_management;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<MemberManagementDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsAssembly(typeof(MemberManagementDbContextPostgresFactory).Assembly.FullName));

        return new MemberManagementDbContext(optionsBuilder.Options);
    }
}
