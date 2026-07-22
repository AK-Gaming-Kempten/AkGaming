using AkGaming.Management.Modules.BoardManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Management.Modules.BoardManagement.Migrations.Postgres;

public sealed class BoardManagementDbContextPostgresFactory : IDesignTimeDbContextFactory<BoardManagementDbContext>
{
    public BoardManagementDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=board_design;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<BoardManagementDbContext>()
            .UseNpgsql(
                connectionString,
                database => database.MigrationsAssembly(typeof(BoardManagementDbContextPostgresFactory).Assembly.FullName))
            .Options;
        return new BoardManagementDbContext(options);
    }
}
