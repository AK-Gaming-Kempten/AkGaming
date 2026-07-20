using AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Management.Modules.InvoiceManagement.Migrations.Postgres;

public sealed class InvoiceManagementDbContextPostgresFactory : IDesignTimeDbContextFactory<InvoiceManagementDbContext>
{
    public InvoiceManagementDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=invoice_management_design;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<InvoiceManagementDbContext>()
            .UseNpgsql(
                connectionString,
                database => database.MigrationsAssembly(typeof(InvoiceManagementDbContextPostgresFactory).Assembly.FullName))
            .Options;
        return new InvoiceManagementDbContext(options);
    }
}
