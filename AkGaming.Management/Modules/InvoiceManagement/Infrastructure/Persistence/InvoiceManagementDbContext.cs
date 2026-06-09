using AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.Modules.InvoiceManagement.Infrastructure.Persistence;

public sealed class InvoiceManagementDbContext(DbContextOptions<InvoiceManagementDbContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceParty> InvoiceParties => Set<InvoiceParty>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<InvoiceBankDetails> InvoiceBankDetails => Set<InvoiceBankDetails>();
    public DbSet<InvoicePartyPreset> InvoicePartyPresets => Set<InvoicePartyPreset>();
    public DbSet<InvoicePaymentTermsPreset> InvoicePaymentTermsPresets => Set<InvoicePaymentTermsPreset>();
    public DbSet<InvoiceBankAccountPreset> InvoiceBankAccountPresets => Set<InvoiceBankAccountPreset>();
    public DbSet<InvoiceLineItemPreset> InvoiceLineItemPresets => Set<InvoiceLineItemPreset>();
    public DbSet<InvoiceLineItemCollectionPreset> InvoiceLineItemCollectionPresets => Set<InvoiceLineItemCollectionPreset>();
    public DbSet<InvoiceLineItemCollectionPresetItem> InvoiceLineItemCollectionPresetItems => Set<InvoiceLineItemCollectionPresetItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceManagementDbContext).Assembly);
    }
}
