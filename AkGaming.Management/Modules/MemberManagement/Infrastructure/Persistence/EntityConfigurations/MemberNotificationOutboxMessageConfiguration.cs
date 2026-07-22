using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence.EntityConfigurations;

public sealed class MemberNotificationOutboxMessageConfiguration : IEntityTypeConfiguration<MemberNotificationOutboxMessage>
{
    public void Configure(EntityTypeBuilder<MemberNotificationOutboxMessage> builder)
    {
        builder.ToTable("MemberNotificationOutbox");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => item.EventId).IsUnique();
        builder.HasIndex(item => new { item.ProcessedAtUtc, item.NextAttemptAtUtc });
        builder.Property(item => item.Type).HasMaxLength(128);
        builder.Property(item => item.LastError).HasMaxLength(4000);
    }
}
