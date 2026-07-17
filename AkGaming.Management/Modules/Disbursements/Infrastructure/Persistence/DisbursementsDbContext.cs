using AkGaming.Management.Modules.Disbursements.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.Modules.Disbursements.Infrastructure.Persistence;

public sealed class DisbursementsDbContext(DbContextOptions<DisbursementsDbContext> options) : DbContext(options)
{
    public DbSet<Reimbursement> Reimbursements => Set<Reimbursement>();
    public DbSet<ExpenseItem> ExpenseItems => Set<ExpenseItem>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<DisbursementEvent> DisbursementEvents => Set<DisbursementEvent>();
    public DbSet<Allocation> Allocations => Set<Allocation>();
    public DbSet<AllocationApplication> AllocationApplications => Set<AllocationApplication>();
    public DbSet<AllocationApproval> AllocationApprovals => Set<AllocationApproval>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reimbursement>(builder =>
        {
            builder.ToTable("DisbursementReimbursements");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.ApplicantName).HasMaxLength(256);
            builder.Property(item => item.Purpose).HasMaxLength(300);
            builder.Property(item => item.Note).HasMaxLength(4000);
            builder.Property(item => item.AdministrativeNote).HasMaxLength(4000);
            builder.OwnsOne(item => item.PaymentMethod, owned =>
            {
                owned.Property(item => item.DisplayName).HasMaxLength(500).HasColumnName("PaymentMethodDisplayName");
                owned.Property(item => item.PaymentInformationId).HasColumnName("PaymentInformationId");
                owned.Property(item => item.Type).HasColumnName("PaymentMethodType");
                owned.Property(item => item.PayPalEmail).HasMaxLength(320).HasColumnName("PaymentMethodPayPalEmail");
                owned.Property(item => item.AccountHolder).HasMaxLength(300).HasColumnName("PaymentMethodAccountHolder");
                owned.Property(item => item.Iban).HasMaxLength(34).HasColumnName("PaymentMethodIban");
                owned.Property(item => item.Bic).HasMaxLength(11).HasColumnName("PaymentMethodBic");
            });
            builder.HasIndex(item => item.UserId);
        });
        modelBuilder.Entity<ExpenseItem>(builder =>
        {
            builder.ToTable("DisbursementExpenseItems");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Description).HasMaxLength(500);
            builder.Property(item => item.Amount).HasPrecision(12, 2);
            builder.HasOne(item => item.Reimbursement).WithMany(item => item.Expenses).HasForeignKey(item => item.ReimbursementId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Receipt>(builder =>
        {
            builder.ToTable("DisbursementReceipts");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.FileName).HasMaxLength(255);
            builder.Property(item => item.ContentType).HasMaxLength(100);
            builder.Property(item => item.StorageKey).HasMaxLength(500);
            builder.HasOne(item => item.ExpenseItem).WithMany(item => item.Receipts).HasForeignKey(item => item.ExpenseItemId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<DisbursementEvent>(builder =>
        {
            builder.ToTable("DisbursementEvents");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Name).HasMaxLength(300);
            builder.Property(item => item.Description).HasMaxLength(4000);
        });
        modelBuilder.Entity<Allocation>(builder =>
        {
            builder.ToTable("DisbursementAllocations");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Name).HasMaxLength(300);
            builder.Property(item => item.Description).HasMaxLength(4000);
            builder.Property(item => item.Amount).HasPrecision(12, 2);
            builder.HasIndex(item => item.ShareToken).IsUnique();
            builder.HasOne(item => item.Event).WithMany(item => item.Allocations).HasForeignKey(item => item.EventId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AllocationApplication>(builder =>
        {
            builder.ToTable("DisbursementAllocationApplications");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.ApplicantName).HasMaxLength(256);
            builder.Property(item => item.Note).HasMaxLength(4000);
            builder.Property(item => item.Amount).HasPrecision(12, 2);
            builder.OwnsOne(item => item.PaymentMethod, owned =>
            {
                owned.Property(item => item.DisplayName).HasMaxLength(500).HasColumnName("PaymentMethodDisplayName");
                owned.Property(item => item.PaymentInformationId).HasColumnName("PaymentInformationId");
                owned.Property(item => item.Type).HasColumnName("PaymentMethodType");
                owned.Property(item => item.PayPalEmail).HasMaxLength(320).HasColumnName("PaymentMethodPayPalEmail");
                owned.Property(item => item.AccountHolder).HasMaxLength(300).HasColumnName("PaymentMethodAccountHolder");
                owned.Property(item => item.Iban).HasMaxLength(34).HasColumnName("PaymentMethodIban");
                owned.Property(item => item.Bic).HasMaxLength(11).HasColumnName("PaymentMethodBic");
            });
            builder.HasIndex(item => item.ApplicantUserId);
            builder.HasOne(item => item.Allocation).WithMany(item => item.Applications).HasForeignKey(item => item.AllocationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AllocationApproval>(builder =>
        {
            builder.ToTable("DisbursementAllocationApprovals");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.ApproverName).HasMaxLength(256);
            builder.HasIndex(item => new { item.ApplicationId, item.ApproverUserId }).IsUnique();
            builder.HasOne(item => item.Application).WithMany(item => item.Approvals).HasForeignKey(item => item.ApplicationId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
