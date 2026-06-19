using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence.EntityConfigurations;

public class PaymentInformationConfiguration : IEntityTypeConfiguration<PaymentInformation> {
    public void Configure(EntityTypeBuilder<PaymentInformation> builder) {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasConversion<int>();
        builder.HasOne(x => x.Member)
            .WithMany(x => x.PaymentInformation)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
