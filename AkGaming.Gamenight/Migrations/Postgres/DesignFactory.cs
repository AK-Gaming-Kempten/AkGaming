using AkGaming.Gamenight.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace AkGaming.Gamenight.Migrations.Postgres;
public sealed class DesignFactory : IDesignTimeDbContextFactory<GamenightDbContext>
{
 public GamenightDbContext CreateDbContext(string[] args)
 {
  var options = new DbContextOptionsBuilder<GamenightDbContext>();
  options.UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__Gamenight") ?? "Host=localhost;Database=gamenight;Username=postgres;Password=postgres", p => p.MigrationsAssembly(typeof(DesignFactory).Assembly.FullName));
  return new GamenightDbContext(options.Options);
 }
}
