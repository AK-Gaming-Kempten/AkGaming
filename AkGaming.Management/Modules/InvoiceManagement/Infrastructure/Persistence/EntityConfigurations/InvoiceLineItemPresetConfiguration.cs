using AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence.EntityConfigurations;

public sealed class InvoiceLineItemPresetConfiguration : IEntityTypeConfiguration<InvoiceLineItemPreset>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItemPreset> builder)
    {
        builder.ToTable("InvoiceLineItemPresets");
        builder.HasKey(preset => preset.Id);
        builder.Property(preset => preset.Label).IsRequired().HasMaxLength(128);
        builder.HasIndex(preset => preset.Label).IsUnique();
        builder.Property(preset => preset.Description).IsRequired().HasMaxLength(1000);
        builder.Property(preset => preset.UnitPrice).HasPrecision(12, 2);
        builder.Property(preset => preset.Quantity).HasPrecision(12, 3);
    }
}
