using AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence.EntityConfigurations;

public sealed class InvoiceLineItemCollectionPresetItemConfiguration : IEntityTypeConfiguration<InvoiceLineItemCollectionPresetItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItemCollectionPresetItem> builder)
    {
        builder.ToTable("InvoiceLineItemCollectionPresetItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Description).IsRequired().HasMaxLength(1000);
        builder.Property(item => item.UnitPrice).HasPrecision(12, 2);
        builder.Property(item => item.Quantity).HasPrecision(12, 3);
        builder.HasIndex(item => new { item.CollectionPresetId, item.SortOrder }).IsUnique();
    }
}
