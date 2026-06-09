using AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Management.Modules.InvoiceManagement.Migrations.Postgres;

public sealed class InvoiceManagementDbContextPostgresFactory : IDesignTimeDbContextFactory<InvoiceManagementDbContext>
{
    public InvoiceManagementDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InvoiceManagementDbContext>();
        options.UseNpgsql("Host=localhost;Database=invoice_management_design;Username=postgres;Password=postgres", npgsql => npgsql.MigrationsAssembly(typeof(InvoiceManagementDbContextPostgresFactory).Assembly.FullName));
        return new InvoiceManagementDbContext(options.Options);
    }
}
