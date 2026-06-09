using AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence.EntityConfigurations;

public sealed class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("InvoiceLineItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Description).IsRequired().HasMaxLength(1000);
        builder.Property(item => item.UnitPrice).HasPrecision(12, 2);
        builder.Property(item => item.Quantity).HasPrecision(12, 3);
        builder.HasIndex(item => new { item.InvoiceId, item.SortOrder }).IsUnique();
    }
}
