using MemberManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemberManagement.Infrastructure.Persistence.EntityConfigurations;

public class MemberAuditLogConfiguration : IEntityTypeConfiguration<MemberAuditLog> {
    public void Configure(EntityTypeBuilder<MemberAuditLog> builder) {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ActionType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasMaxLength(128);
    }
}
