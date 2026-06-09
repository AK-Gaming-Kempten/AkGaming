using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;
using AkGaming.Management.Modules.InvoiceManagement.Domain.Enums;
using RenderBankDetails = AkGaming.InvoiceGenerator.Core.Models.InvoiceBankDetails;
using RenderDocument = AkGaming.InvoiceGenerator.Core.Models.InvoiceDocument;
using RenderLineItem = AkGaming.InvoiceGenerator.Core.Models.InvoiceLineItem;
using RenderParty = AkGaming.InvoiceGenerator.Core.Models.InvoiceParty;

namespace AkGaming.Management.Modules.InvoiceManagement.Application.Mapping;

public static class InvoiceMappingExtensions
{
    public static InvoiceDetailsDto ToDto(this Invoice invoice)
    {
        var seller = invoice.Parties.Single(party => party.Role == InvoicePartyRole.Seller);
        var buyer = invoice.Parties.Single(party => party.Role == InvoicePartyRole.Buyer);

        return new InvoiceDetailsDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            ServiceDate = invoice.ServiceDate,
            Seller = seller.ToDto(),
            Buyer = buyer.ToDto(),
            IntroText = invoice.IntroText,
            BodyText = invoice.BodyText,
            LineItems = invoice.LineItems
                .OrderBy(item => item.SortOrder)
                .Select(item => new InvoiceLineItemDto
                {
                    Id = item.Id,
                    Description = item.Description,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity
                })
                .ToList(),
            PaymentTerms = invoice.PaymentTerms,
            BankDetails = invoice.BankDetails is null
                ? new InvoiceBankDetailsDto()
                : new InvoiceBankDetailsDto
                {
                    Iban = invoice.BankDetails.Iban,
                    Bic = invoice.BankDetails.Bic,
                    Blz = invoice.BankDetails.Blz,
                    AccountHolder = invoice.BankDetails.AccountHolder
                },
            ClosingText = invoice.ClosingText,
            SignatureName = invoice.SignatureName,
            Greeting = invoice.Greeting,
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt
        };
    }

    public static InvoiceSummaryDto ToSummaryDto(this Invoice invoice)
    {
        var buyerName = invoice.Parties.FirstOrDefault(party => party.Role == InvoicePartyRole.Buyer)?.Name ?? string.Empty;
        var total = invoice.LineItems.Sum(item => item.UnitPrice * item.Quantity);
        return new InvoiceSummaryDto(invoice.Id, invoice.InvoiceNumber, invoice.InvoiceDate, buyerName, total, invoice.UpdatedAt);
    }

    public static InvoicePartyPresetDto ToDto(this InvoicePartyPreset preset)
    {
        return new InvoicePartyPresetDto
        {
            Id = preset.Id,
            Label = preset.Label,
            Party = new InvoicePartyDto
            {
                Name = preset.Name,
                Street = preset.Street,
                PostalCode = preset.PostalCode,
                City = preset.City,
                Country = preset.Country
            },
            CreatedAt = preset.CreatedAt,
            UpdatedAt = preset.UpdatedAt
        };
    }

    public static InvoicePaymentTermsPresetDto ToDto(this InvoicePaymentTermsPreset preset)
    {
        return new InvoicePaymentTermsPresetDto
        {
            Id = preset.Id,
            Label = preset.Label,
            Terms = preset.Terms,
            CreatedAt = preset.CreatedAt,
            UpdatedAt = preset.UpdatedAt
        };
    }

    public static InvoiceBankAccountPresetDto ToDto(this InvoiceBankAccountPreset preset)
    {
        return new InvoiceBankAccountPresetDto
        {
            Id = preset.Id,
            Label = preset.Label,
            BankDetails = new InvoiceBankDetailsDto
            {
                Iban = preset.Iban,
                Bic = preset.Bic,
                Blz = preset.Blz,
                AccountHolder = preset.AccountHolder
            },
            CreatedAt = preset.CreatedAt,
            UpdatedAt = preset.UpdatedAt
        };
    }

    public static InvoiceLineItemPresetDto ToDto(this InvoiceLineItemPreset preset)
    {
        return new InvoiceLineItemPresetDto
        {
            Id = preset.Id,
            Label = preset.Label,
            LineItem = new InvoiceLineItemDto
            {
                Description = preset.Description,
                UnitPrice = preset.UnitPrice,
                Quantity = preset.Quantity
            },
            CreatedAt = preset.CreatedAt,
            UpdatedAt = preset.UpdatedAt
        };
    }

    public static InvoiceLineItemCollectionPresetDto ToDto(this InvoiceLineItemCollectionPreset preset)
    {
        return new InvoiceLineItemCollectionPresetDto
        {
            Id = preset.Id,
            Label = preset.Label,
            LineItems = preset.LineItems
                .OrderBy(item => item.SortOrder)
                .Select(item => new InvoiceLineItemDto
                {
                    Description = item.Description,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity
                })
                .ToList(),
            CreatedAt = preset.CreatedAt,
            UpdatedAt = preset.UpdatedAt
        };
    }

    public static RenderDocument ToRenderDocument(this InvoiceDetailsDto invoice)
    {
        var bankDetails = invoice.BankDetails;
        var hasBankDetails = bankDetails is not null
            && (!string.IsNullOrWhiteSpace(bankDetails.Iban)
                || !string.IsNullOrWhiteSpace(bankDetails.Bic)
                || !string.IsNullOrWhiteSpace(bankDetails.Blz)
                || !string.IsNullOrWhiteSpace(bankDetails.AccountHolder));

        return new RenderDocument
        {
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            ServiceDate = invoice.ServiceDate,
            Seller = invoice.Seller.ToRenderParty(),
            Buyer = invoice.Buyer.ToRenderParty(),
            IntroText = invoice.IntroText,
            BodyText = invoice.BodyText,
            LineItems = invoice.LineItems.Select(item => new RenderLineItem
            {
                Description = item.Description,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity
            }).ToList(),
            PaymentTerms = invoice.PaymentTerms,
            BankDetails = hasBankDetails
                ? new RenderBankDetails
                {
                    Iban = bankDetails!.Iban,
                    Bic = bankDetails.Bic,
                    Blz = bankDetails.Blz,
                    AccountHolder = bankDetails.AccountHolder
                }
                : null,
            ClosingText = invoice.ClosingText,
            SignatureName = invoice.SignatureName,
            Greeting = invoice.Greeting
        };
    }

    private static InvoicePartyDto ToDto(this InvoiceParty party)
    {
        return new InvoicePartyDto
        {
            Name = party.Name,
            Street = party.Street,
            PostalCode = party.PostalCode,
            City = party.City,
            Country = party.Country
        };
    }

    private static RenderParty ToRenderParty(this InvoicePartyDto party)
    {
        return new RenderParty
        {
            Name = party.Name,
            Street = party.Street,
            PostalCode = party.PostalCode,
            City = party.City,
            Country = party.Country
        };
    }
}
