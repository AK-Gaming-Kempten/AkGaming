using AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Management.Modules.InvoiceManagement.Migrations.Sqlite;

public sealed class InvoiceManagementDbContextSqliteFactory : IDesignTimeDbContextFactory<InvoiceManagementDbContext>
{
    public InvoiceManagementDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InvoiceManagementDbContext>();
        options.UseSqlite("Data Source=invoice-management-design.db", sqlite => sqlite.MigrationsAssembly(typeof(InvoiceManagementDbContextSqliteFactory).Assembly.FullName));
        return new InvoiceManagementDbContext(options.Options);
    }
}
