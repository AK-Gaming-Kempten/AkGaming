using AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence.EntityConfigurations;

public sealed class InvoicePartyPresetConfiguration : IEntityTypeConfiguration<InvoicePartyPreset>
{
    public void Configure(EntityTypeBuilder<InvoicePartyPreset> builder)
    {
        builder.ToTable("InvoicePartyPresets");
        builder.HasKey(preset => preset.Id);
        builder.Property(preset => preset.Label).IsRequired().HasMaxLength(128);
        builder.HasIndex(preset => preset.Label).IsUnique();
        builder.Property(preset => preset.Name).IsRequired().HasMaxLength(256);
        builder.Property(preset => preset.Street).IsRequired().HasMaxLength(256);
        builder.Property(preset => preset.PostalCode).IsRequired().HasMaxLength(32);
        builder.Property(preset => preset.City).IsRequired().HasMaxLength(128);
        builder.Property(preset => preset.Country).HasMaxLength(128);
    }
}
