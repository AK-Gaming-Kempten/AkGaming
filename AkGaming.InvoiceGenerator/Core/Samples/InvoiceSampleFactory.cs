using AkGaming.Core.Constants;
using AkGaming.InvoiceGenerator.Core.Models;

namespace AkGaming.InvoiceGenerator.Core.Samples;

public static class InvoiceSampleFactory
{
    public static InvoiceDocument CreateSponsoringSample()
        => new()
        {
            InvoiceNumber = "2026-001",
            InvoiceDate = new DateOnly(2026, 03, 27),
            ServiceDate = new DateOnly(2026, 03, 15),
            Seller = new InvoiceParty
            {
                Name = ClubConstants.Organization.LegalName,
                Street = ClubConstants.Address.Street,
                PostalCode = ClubConstants.Address.PostalCode,
                City = ClubConstants.Address.City
            },
            Buyer = new InvoiceParty
            {
                Name = "Soloplan GmbH",
                Street = "Illerhöhe 1",
                PostalCode = "87437",
                City = "Kempten"
            },
            LineItems =
            [
                new InvoiceLineItem { Description = "Verpflegung Game Night", UnitPrice = 275.00m, Quantity = 2m },
                new InvoiceLineItem { Description = "Technische Infrastruktur / Hosting", UnitPrice = 115.00m, Quantity = 1m },
                new InvoiceLineItem { Description = "Preisgeld Online Turnier", UnitPrice = 300.00m, Quantity = 1m },
                new InvoiceLineItem { Description = "Gelder fuer E-Sports", UnitPrice = 400.00m, Quantity = 1m },
                new InvoiceLineItem { Description = "Game Jam", UnitPrice = 300.00m, Quantity = 1m }
            ],
            PaymentTerms = "Zahlung innerhalb von 14 Tagen ab Rechnungseingang ohne Abzuege auf folgende Konto:",
            BankDetails = new InvoiceBankDetails
            {
                Iban = ClubConstants.BankAccount.Iban,
                Blz = ClubConstants.BankAccount.Blz,
                Bic = ClubConstants.BankAccount.Bic,
                AccountHolder = ClubConstants.BankAccount.AccountHolder
            },
            SignatureName = ClubConstants.Contacts.FirstChair.Name
        };
}
