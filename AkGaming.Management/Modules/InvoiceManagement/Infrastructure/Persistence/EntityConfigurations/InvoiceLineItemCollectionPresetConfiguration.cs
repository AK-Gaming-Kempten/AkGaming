using AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence.EntityConfigurations;

public sealed class InvoiceLineItemCollectionPresetConfiguration : IEntityTypeConfiguration<InvoiceLineItemCollectionPreset>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItemCollectionPreset> builder)
    {
        builder.ToTable("InvoiceLineItemCollectionPresets");
        builder.HasKey(preset => preset.Id);
        builder.Property(preset => preset.Label).IsRequired().HasMaxLength(128);
        builder.HasIndex(preset => preset.Label).IsUnique();
        builder.HasMany(preset => preset.LineItems)
            .WithOne(item => item.CollectionPreset)
            .HasForeignKey(item => item.CollectionPresetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
