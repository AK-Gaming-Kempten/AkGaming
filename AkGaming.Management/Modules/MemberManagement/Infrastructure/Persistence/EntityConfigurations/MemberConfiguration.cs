using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence.EntityConfigurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member> {
    public void Configure(EntityTypeBuilder<Member> builder) {
        builder.HasKey(m => m.Id);

        builder.OwnsOne(m => m.Address);

        builder.HasMany(m => m.StatusChanges)
            .WithOne(sc => sc.Member)
            .HasForeignKey(sc => sc.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(m => m.Status)
            .HasConversion<int>();
    }
}