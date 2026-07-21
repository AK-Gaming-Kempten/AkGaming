using AkGaming.Management.Modules.BoardManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Management.Modules.BoardManagement.Migrations.Sqlite;

public sealed class BoardManagementDbContextSqliteFactory : IDesignTimeDbContextFactory<BoardManagementDbContext>
{
    public BoardManagementDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BoardManagementDbContext>().UseSqlite("Data Source=board_design.db", database => database.MigrationsAssembly(typeof(BoardManagementDbContextSqliteFactory).Assembly.FullName)).Options;
        return new BoardManagementDbContext(options);
    }
}
