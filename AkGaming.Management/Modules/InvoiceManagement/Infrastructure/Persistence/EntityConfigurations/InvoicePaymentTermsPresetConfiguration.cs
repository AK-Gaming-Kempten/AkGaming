using AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence.EntityConfigurations;

public sealed class InvoicePaymentTermsPresetConfiguration : IEntityTypeConfiguration<InvoicePaymentTermsPreset>
{
    public void Configure(EntityTypeBuilder<InvoicePaymentTermsPreset> builder)
    {
        builder.ToTable("InvoicePaymentTermsPresets");
        builder.HasKey(preset => preset.Id);
        builder.Property(preset => preset.Label).IsRequired().HasMaxLength(128);
        builder.HasIndex(preset => preset.Label).IsUnique();
        builder.Property(preset => preset.Terms).IsRequired().HasMaxLength(2000);
    }
}
