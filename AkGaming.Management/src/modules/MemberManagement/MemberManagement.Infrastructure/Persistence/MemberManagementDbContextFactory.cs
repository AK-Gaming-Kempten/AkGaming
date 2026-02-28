using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MemberManagement.Infrastructure.Persistence;

public class MemberManagementDbContextFactory : IDesignTimeDbContextFactory<MemberManagementDbContext> {
    public MemberManagementDbContext CreateDbContext(string[] args) {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                               ?? throw new InvalidOperationException("Missing env var ConnectionStrings__DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<MemberManagementDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new MemberManagementDbContext(optionsBuilder.Options);
    }
}