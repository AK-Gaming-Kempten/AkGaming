using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;

/// <summary>
/// The database context for the member management module.
/// </summary>
public class MemberManagementDbContext : DbContext {
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberManagementDbContext" /> class.
    /// </summary>
    /// <param name="options"></param>
    public MemberManagementDbContext(DbContextOptions<MemberManagementDbContext> options)
        : base(options) { }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<MembershipStatusChangeEvent> MembershipStatusChangeEvents => Set<MembershipStatusChangeEvent>();
    public DbSet<MemberLinkingRequest> MemberLinkingRequests => Set<MemberLinkingRequest>();
    public DbSet<MembershipApplicationRequest> MembershipApplicationRequests => Set<MembershipApplicationRequest>();
    public DbSet<MemberAuditLog> MemberAuditLogs => Set<MemberAuditLog>();
    public DbSet<MembershipPaymentPeriod> MembershipPaymentPeriods => Set<MembershipPaymentPeriod>();
    public DbSet<MembershipDue> MembershipDues => Set<MembershipDue>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MemberManagementDbContext).Assembly);
    }
}
