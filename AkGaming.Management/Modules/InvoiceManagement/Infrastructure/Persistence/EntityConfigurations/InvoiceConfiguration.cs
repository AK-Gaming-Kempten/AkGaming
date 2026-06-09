using AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence.EntityConfigurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(invoice => invoice.Id);
        builder.Property(invoice => invoice.InvoiceNumber).IsRequired().HasMaxLength(64);
        builder.HasIndex(invoice => invoice.InvoiceNumber).IsUnique();
        builder.Property(invoice => invoice.IntroText).IsRequired().HasMaxLength(1000);
        builder.Property(invoice => invoice.BodyText).IsRequired().HasMaxLength(4000);
        builder.Property(invoice => invoice.PaymentTerms).HasMaxLength(2000);
        builder.Property(invoice => invoice.ClosingText).IsRequired().HasMaxLength(2000);
        builder.Property(invoice => invoice.SignatureName).IsRequired().HasMaxLength(256);
        builder.Property(invoice => invoice.Greeting).IsRequired().HasMaxLength(256);
        builder.HasMany(invoice => invoice.Parties).WithOne(party => party.Invoice).HasForeignKey(party => party.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(invoice => invoice.LineItems).WithOne(item => item.Invoice).HasForeignKey(item => item.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(invoice => invoice.BankDetails).WithOne(bank => bank.Invoice).HasForeignKey<InvoiceBankDetails>(bank => bank.InvoiceId).OnDelete(DeleteBehavior.Cascade);
    }
}
