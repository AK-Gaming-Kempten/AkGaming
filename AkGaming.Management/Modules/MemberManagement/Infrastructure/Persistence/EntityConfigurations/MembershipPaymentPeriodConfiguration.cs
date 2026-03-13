using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence.EntityConfigurations;

public class MembershipPaymentPeriodConfiguration : IEntityTypeConfiguration<MembershipPaymentPeriod> {
    public void Configure(EntityTypeBuilder<MembershipPaymentPeriod> builder) {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.DefaultDueAmount)
            .HasPrecision(10, 2);

        builder.HasMany(x => x.Dues)
            .WithOne(x => x.PaymentPeriod)
            .HasForeignKey(x => x.PaymentPeriodId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
