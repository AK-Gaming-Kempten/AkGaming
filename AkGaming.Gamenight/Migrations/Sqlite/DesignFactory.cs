using AkGaming.Gamenight.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace AkGaming.Gamenight.Migrations.Sqlite;
public sealed class DesignFactory : IDesignTimeDbContextFactory<GamenightDbContext>
{
 public GamenightDbContext CreateDbContext(string[] args)
 {
  var options = new DbContextOptionsBuilder<GamenightDbContext>();
  options.UseSqlite(Environment.GetEnvironmentVariable("ConnectionStrings__Gamenight") ?? "Data Source=gamenight.db", p => p.MigrationsAssembly(typeof(DesignFactory).Assembly.FullName));
  return new GamenightDbContext(options.Options);
 }
}
