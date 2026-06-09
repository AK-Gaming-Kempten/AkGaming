using AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence.EntityConfigurations;

public sealed class InvoicePartyConfiguration : IEntityTypeConfiguration<InvoiceParty>
{
    public void Configure(EntityTypeBuilder<InvoiceParty> builder)
    {
        builder.ToTable("InvoiceParties");
        builder.HasKey(party => party.Id);
        builder.HasIndex(party => new { party.InvoiceId, party.Role }).IsUnique();
        builder.Property(party => party.Name).IsRequired().HasMaxLength(256);
        builder.Property(party => party.Street).IsRequired().HasMaxLength(256);
        builder.Property(party => party.PostalCode).IsRequired().HasMaxLength(32);
        builder.Property(party => party.City).IsRequired().HasMaxLength(128);
        builder.Property(party => party.Country).HasMaxLength(128);
    }
}
