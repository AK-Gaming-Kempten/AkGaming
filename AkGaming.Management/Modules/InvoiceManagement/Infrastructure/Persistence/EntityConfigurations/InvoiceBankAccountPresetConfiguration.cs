using AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence.EntityConfigurations;

public sealed class InvoiceBankAccountPresetConfiguration : IEntityTypeConfiguration<InvoiceBankAccountPreset>
{
    public void Configure(EntityTypeBuilder<InvoiceBankAccountPreset> builder)
    {
        builder.ToTable("InvoiceBankAccountPresets");
        builder.HasKey(preset => preset.Id);
        builder.Property(preset => preset.Label).IsRequired().HasMaxLength(128);
        builder.HasIndex(preset => preset.Label).IsUnique();
        builder.Property(preset => preset.Iban).HasMaxLength(64);
        builder.Property(preset => preset.Bic).HasMaxLength(32);
        builder.Property(preset => preset.Blz).HasMaxLength(32);
        builder.Property(preset => preset.AccountHolder).HasMaxLength(256);
    }
}
