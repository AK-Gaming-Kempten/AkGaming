using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence.EntityConfigurations;

public class MembershipDueConfiguration : IEntityTypeConfiguration<MembershipDue> {
    public void Configure(EntityTypeBuilder<MembershipDue> builder) {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.Property(x => x.LastReminderSendStatus)
            .HasConversion<int>();

        builder.Property(x => x.DueAmount)
            .HasPrecision(10, 2);

        builder.Property(x => x.PaidAmount)
            .HasPrecision(10, 2);

        builder.Property(x => x.SettlementReference)
            .HasMaxLength(256);

        builder.HasIndex(x => new { x.PaymentPeriodId, x.MemberId })
            .IsUnique();
    }
}
