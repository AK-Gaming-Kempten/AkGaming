using AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence.EntityConfigurations;

public sealed class InvoiceBankDetailsConfiguration : IEntityTypeConfiguration<InvoiceBankDetails>
{
    public void Configure(EntityTypeBuilder<InvoiceBankDetails> builder)
    {
        builder.ToTable("InvoiceBankDetails");
        builder.HasKey(bank => bank.Id);
        builder.HasIndex(bank => bank.InvoiceId).IsUnique();
        builder.Property(bank => bank.Iban).HasMaxLength(64);
        builder.Property(bank => bank.Bic).HasMaxLength(32);
        builder.Property(bank => bank.Blz).HasMaxLength(32);
        builder.Property(bank => bank.AccountHolder).HasMaxLength(256);
    }
}
