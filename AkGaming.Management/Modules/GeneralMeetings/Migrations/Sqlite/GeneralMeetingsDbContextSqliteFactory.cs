using AkGaming.Management.Modules.GeneralMeetings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Management.Modules.GeneralMeetings.Migrations.Sqlite;
public sealed class GeneralMeetingsDbContextSqliteFactory : IDesignTimeDbContextFactory<GeneralMeetingsDbContext>
{
    public GeneralMeetingsDbContext CreateDbContext(string[] args) { var builder = new DbContextOptionsBuilder<GeneralMeetingsDbContext>(); builder.UseSqlite("Data Source=general-meetings.design.db", x => x.MigrationsAssembly(typeof(GeneralMeetingsDbContextSqliteFactory).Assembly.FullName)); return new GeneralMeetingsDbContext(builder.Options); }
}
