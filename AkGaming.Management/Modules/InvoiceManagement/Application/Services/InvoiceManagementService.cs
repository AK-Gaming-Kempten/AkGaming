using AkGaming.Core.Common.Generics;
using AkGaming.Core.Constants;
using AkGaming.InvoiceGenerator.Core.Rendering;
using AkGaming.Management.Modules.InvoiceManagement.Application.Interfaces;
using AkGaming.Management.Modules.InvoiceManagement.Application.Mapping;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.Services;
using AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;
using AkGaming.Management.Modules.InvoiceManagement.Domain.Enums;

namespace AkGaming.Management.Modules.InvoiceManagement.Application.Services;

public sealed class InvoiceManagementService(
    IInvoiceRepository repository,
    IInvoiceHtmlRenderer htmlRenderer,
    IInvoicePdfRenderer pdfRenderer) : IInvoiceManagementService
{
    public async Task<Result<IReadOnlyList<InvoiceSummaryDto>>> GetInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await repository.GetAllAsync(cancellationToken);
        var result = invoices.Select(invoice => invoice.ToSummaryDto()).ToList();
        return Result<IReadOnlyList<InvoiceSummaryDto>>.Success(result);
    }

    public async Task<Result<InvoiceDetailsDto>> GetInvoiceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await repository.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
            return Result<InvoiceDetailsDto>.Failure("Invoice not found.");

        return Result<InvoiceDetailsDto>.Success(invoice.ToDto());
    }

    public async Task<Result<InvoiceDetailsDto>> CreateInvoiceAsync(InvoiceDetailsDto request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateInvoice(request);
        if (!validation.IsSuccess)
            return Result<InvoiceDetailsDto>.Failure(validation.Error!);

        if (await repository.InvoiceNumberExistsAsync(request.InvoiceNumber.Trim(), cancellationToken: cancellationToken))
            return Result<InvoiceDetailsDto>.Failure("An invoice with this invoice number already exists.");

        var now = DateTimeOffset.UtcNow;
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now
        };
        ApplyRequest(invoice, request, now);
        repository.Add(invoice);
        await repository.SaveChangesAsync(cancellationToken);

        return Result<InvoiceDetailsDto>.Success(invoice.ToDto());
    }

    public async Task<Result<InvoiceDetailsDto>> UpdateInvoiceAsync(Guid id, InvoiceDetailsDto request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateInvoice(request);
        if (!validation.IsSuccess)
            return Result<InvoiceDetailsDto>.Failure(validation.Error!);

        var invoice = await repository.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
            return Result<InvoiceDetailsDto>.Failure("Invoice not found.");

        if (await repository.InvoiceNumberExistsAsync(request.InvoiceNumber.Trim(), id, cancellationToken))
            return Result<InvoiceDetailsDto>.Failure("An invoice with this invoice number already exists.");

        ApplyRequest(invoice, request, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);

        return Result<InvoiceDetailsDto>.Success(invoice.ToDto());
    }

    public async Task<Result> DeleteInvoiceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await repository.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
            return Result.Failure("Invoice not found.");

        repository.Remove(invoice);
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<string>> RenderHtmlAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await GetInvoiceAsync(id, cancellationToken);
        return result.IsSuccess
            ? RenderHtml(result.Value!)
            : Result<string>.Failure(result.Error!);
    }

    public async Task<Result<byte[]>> RenderPdfAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await GetInvoiceAsync(id, cancellationToken);
        return result.IsSuccess
            ? RenderPdf(result.Value!)
            : Result<byte[]>.Failure(result.Error!);
    }

    public Result<string> RenderHtml(InvoiceDetailsDto request)
    {
        var validation = ValidateInvoice(request);
        if (!validation.IsSuccess)
            return Result<string>.Failure(validation.Error!);

        var html = htmlRenderer.Render(request.ToRenderDocument());
        return Result<string>.Success(html);
    }

    public Result<byte[]> RenderPdf(InvoiceDetailsDto request)
    {
        var validation = ValidateInvoice(request);
        if (!validation.IsSuccess)
            return Result<byte[]>.Failure(validation.Error!);

        var pdf = pdfRenderer.Render(request.ToRenderDocument());
        return Result<byte[]>.Success(pdf);
    }

    public async Task<Result<IReadOnlyList<InvoicePartyPresetDto>>> GetPartyPresetsAsync(CancellationToken cancellationToken = default)
    {
        var presets = await repository.GetPartyPresetsAsync(cancellationToken);
        var result = presets.Select(preset => preset.ToDto()).ToList();
        return Result<IReadOnlyList<InvoicePartyPresetDto>>.Success(result);
    }

    public async Task<Result<InvoicePartyPresetDto>> CreatePartyPresetAsync(InvoicePartyPresetDto request, CancellationToken cancellationToken = default)
    {
        var validation = ValidatePartyPreset(request);
        if (!validation.IsSuccess)
            return Result<InvoicePartyPresetDto>.Failure(validation.Error!);

        if (await repository.PartyPresetLabelExistsAsync(request.Label.Trim(), cancellationToken: cancellationToken))
            return Result<InvoicePartyPresetDto>.Failure("A party preset with this label already exists.");

        var now = DateTimeOffset.UtcNow;
        var preset = new InvoicePartyPreset { Id = Guid.NewGuid(), CreatedAt = now };
        ApplyPresetRequest(preset, request, now);
        repository.AddPartyPreset(preset);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<InvoicePartyPresetDto>.Success(preset.ToDto());
    }

    public async Task<Result<InvoicePartyPresetDto>> UpdatePartyPresetAsync(Guid id, InvoicePartyPresetDto request, CancellationToken cancellationToken = default)
    {
        var validation = ValidatePartyPreset(request);
        if (!validation.IsSuccess)
            return Result<InvoicePartyPresetDto>.Failure(validation.Error!);

        var preset = await repository.GetPartyPresetAsync(id, cancellationToken);
        if (preset is null)
            return Result<InvoicePartyPresetDto>.Failure("Invoice party preset not found.");

        if (await repository.PartyPresetLabelExistsAsync(request.Label.Trim(), id, cancellationToken))
            return Result<InvoicePartyPresetDto>.Failure("A party preset with this label already exists.");

        ApplyPresetRequest(preset, request, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<InvoicePartyPresetDto>.Success(preset.ToDto());
    }

    public async Task<Result> DeletePartyPresetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var preset = await repository.GetPartyPresetAsync(id, cancellationToken);
        if (preset is null)
            return Result.Failure("Invoice party preset not found.");

        repository.RemovePartyPreset(preset);
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<InvoicePaymentTermsPresetDto>>> GetPaymentTermsPresetsAsync(CancellationToken cancellationToken = default)
    {
        var presets = await repository.GetPaymentTermsPresetsAsync(cancellationToken);
        var result = presets.Select(preset => preset.ToDto()).ToList();
        return Result<IReadOnlyList<InvoicePaymentTermsPresetDto>>.Success(result);
    }

    public async Task<Result<InvoicePaymentTermsPresetDto>> CreatePaymentTermsPresetAsync(InvoicePaymentTermsPresetDto request, CancellationToken cancellationToken = default)
    {
        var validation = ValidatePaymentTermsPreset(request);
        if (!validation.IsSuccess)
            return Result<InvoicePaymentTermsPresetDto>.Failure(validation.Error!);

        if (await repository.PaymentTermsPresetLabelExistsAsync(request.Label.Trim(), cancellationToken: cancellationToken))
            return Result<InvoicePaymentTermsPresetDto>.Failure("A payment terms preset with this label already exists.");

        var now = DateTimeOffset.UtcNow;
        var preset = new InvoicePaymentTermsPreset
        {
            Id = Guid.NewGuid(),
            CreatedAt = now
        };
        ApplyPaymentTermsPresetRequest(preset, request, now);
        repository.AddPaymentTermsPreset(preset);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<InvoicePaymentTermsPresetDto>.Success(preset.ToDto());
    }

    public async Task<Result<InvoicePaymentTermsPresetDto>> UpdatePaymentTermsPresetAsync(Guid id, InvoicePaymentTermsPresetDto request, CancellationToken cancellationToken = default)
    {
        var validation = ValidatePaymentTermsPreset(request);
        if (!validation.IsSuccess)
            return Result<InvoicePaymentTermsPresetDto>.Failure(validation.Error!);

        var preset = await repository.GetPaymentTermsPresetAsync(id, cancellationToken);
        if (preset is null)
            return Result<InvoicePaymentTermsPresetDto>.Failure("Payment terms preset not found.");

        if (await repository.PaymentTermsPresetLabelExistsAsync(request.Label.Trim(), id, cancellationToken))
            return Result<InvoicePaymentTermsPresetDto>.Failure("A payment terms preset with this label already exists.");

        ApplyPaymentTermsPresetRequest(preset, request, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<InvoicePaymentTermsPresetDto>.Success(preset.ToDto());
    }

    public async Task<Result> DeletePaymentTermsPresetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var preset = await repository.GetPaymentTermsPresetAsync(id, cancellationToken);
        if (preset is null)
            return Result.Failure("Payment terms preset not found.");

        repository.RemovePaymentTermsPreset(preset);
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<InvoiceBankAccountPresetDto>>> GetBankAccountPresetsAsync(CancellationToken cancellationToken = default)
    {
        var presets = await repository.GetBankAccountPresetsAsync(cancellationToken);
        return Result<IReadOnlyList<InvoiceBankAccountPresetDto>>.Success(presets.Select(preset => preset.ToDto()).ToList());
    }

    public async Task<Result<InvoiceBankAccountPresetDto>> CreateBankAccountPresetAsync(InvoiceBankAccountPresetDto request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateBankAccountPreset(request);
        if (!validation.IsSuccess)
            return Result<InvoiceBankAccountPresetDto>.Failure(validation.Error!);
        if (await repository.BankAccountPresetLabelExistsAsync(request.Label.Trim(), cancellationToken: cancellationToken))
            return Result<InvoiceBankAccountPresetDto>.Failure("A bank account preset with this label already exists.");

        var now = DateTimeOffset.UtcNow;
        var preset = new InvoiceBankAccountPreset { Id = Guid.NewGuid(), CreatedAt = now };
        ApplyBankAccountPresetRequest(preset, request, now);
        repository.AddBankAccountPreset(preset);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<InvoiceBankAccountPresetDto>.Success(preset.ToDto());
    }

    public async Task<Result<InvoiceBankAccountPresetDto>> UpdateBankAccountPresetAsync(Guid id, InvoiceBankAccountPresetDto request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateBankAccountPreset(request);
        if (!validation.IsSuccess)
            return Result<InvoiceBankAccountPresetDto>.Failure(validation.Error!);
        var preset = await repository.GetBankAccountPresetAsync(id, cancellationToken);
        if (preset is null)
            return Result<InvoiceBankAccountPresetDto>.Failure("Bank account preset not found.");
        if (await repository.BankAccountPresetLabelExistsAsync(request.Label.Trim(), id, cancellationToken))
            return Result<InvoiceBankAccountPresetDto>.Failure("A bank account preset with this label already exists.");

        ApplyBankAccountPresetRequest(preset, request, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<InvoiceBankAccountPresetDto>.Success(preset.ToDto());
    }

    public async Task<Result> DeleteBankAccountPresetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var preset = await repository.GetBankAccountPresetAsync(id, cancellationToken);
        if (preset is null)
            return Result.Failure("Bank account preset not found.");
        repository.RemoveBankAccountPreset(preset);
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<InvoiceLineItemPresetDto>>> GetLineItemPresetsAsync(CancellationToken cancellationToken = default)
    {
        var presets = await repository.GetLineItemPresetsAsync(cancellationToken);
        return Result<IReadOnlyList<InvoiceLineItemPresetDto>>.Success(presets.Select(preset => preset.ToDto()).ToList());
    }

    public async Task<Result<InvoiceLineItemPresetDto>> CreateLineItemPresetAsync(InvoiceLineItemPresetDto request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateLineItemPreset(request);
        if (!validation.IsSuccess)
            return Result<InvoiceLineItemPresetDto>.Failure(validation.Error!);
        if (await repository.LineItemPresetLabelExistsAsync(request.Label.Trim(), cancellationToken: cancellationToken))
            return Result<InvoiceLineItemPresetDto>.Failure("A line item preset with this label already exists.");

        var now = DateTimeOffset.UtcNow;
        var preset = new InvoiceLineItemPreset { Id = Guid.NewGuid(), CreatedAt = now };
        ApplyLineItemPresetRequest(preset, request, now);
        repository.AddLineItemPreset(preset);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<InvoiceLineItemPresetDto>.Success(preset.ToDto());
    }

    public async Task<Result<InvoiceLineItemPresetDto>> UpdateLineItemPresetAsync(Guid id, InvoiceLineItemPresetDto request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateLineItemPreset(request);
        if (!validation.IsSuccess)
            return Result<InvoiceLineItemPresetDto>.Failure(validation.Error!);
        var preset = await repository.GetLineItemPresetAsync(id, cancellationToken);
        if (preset is null)
            return Result<InvoiceLineItemPresetDto>.Failure("Line item preset not found.");
        if (await repository.LineItemPresetLabelExistsAsync(request.Label.Trim(), id, cancellationToken))
            return Result<InvoiceLineItemPresetDto>.Failure("A line item preset with this label already exists.");

        ApplyLineItemPresetRequest(preset, request, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<InvoiceLineItemPresetDto>.Success(preset.ToDto());
    }

    public async Task<Result> DeleteLineItemPresetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var preset = await repository.GetLineItemPresetAsync(id, cancellationToken);
        if (preset is null)
            return Result.Failure("Line item preset not found.");
        repository.RemoveLineItemPreset(preset);
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<InvoiceLineItemCollectionPresetDto>>> GetLineItemCollectionPresetsAsync(CancellationToken cancellationToken = default)
    {
        var presets = await repository.GetLineItemCollectionPresetsAsync(cancellationToken);
        return Result<IReadOnlyList<InvoiceLineItemCollectionPresetDto>>.Success(presets.Select(preset => preset.ToDto()).ToList());
    }

    public async Task<Result<InvoiceLineItemCollectionPresetDto>> CreateLineItemCollectionPresetAsync(InvoiceLineItemCollectionPresetDto request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateLineItemCollectionPreset(request);
        if (!validation.IsSuccess)
            return Result<InvoiceLineItemCollectionPresetDto>.Failure(validation.Error!);
        if (await repository.LineItemCollectionPresetLabelExistsAsync(request.Label.Trim(), cancellationToken: cancellationToken))
            return Result<InvoiceLineItemCollectionPresetDto>.Failure("A line item collection preset with this label already exists.");

        var now = DateTimeOffset.UtcNow;
        var preset = new InvoiceLineItemCollectionPreset { Id = Guid.NewGuid(), CreatedAt = now };
        ApplyLineItemCollectionPresetRequest(preset, request, now);
        repository.AddLineItemCollectionPreset(preset);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<InvoiceLineItemCollectionPresetDto>.Success(preset.ToDto());
    }

    public async Task<Result<InvoiceLineItemCollectionPresetDto>> UpdateLineItemCollectionPresetAsync(Guid id, InvoiceLineItemCollectionPresetDto request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateLineItemCollectionPreset(request);
        if (!validation.IsSuccess)
            return Result<InvoiceLineItemCollectionPresetDto>.Failure(validation.Error!);
        var preset = await repository.GetLineItemCollectionPresetAsync(id, cancellationToken);
        if (preset is null)
            return Result<InvoiceLineItemCollectionPresetDto>.Failure("Line item collection preset not found.");
        if (await repository.LineItemCollectionPresetLabelExistsAsync(request.Label.Trim(), id, cancellationToken))
            return Result<InvoiceLineItemCollectionPresetDto>.Failure("A line item collection preset with this label already exists.");

        ApplyLineItemCollectionPresetRequest(preset, request, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<InvoiceLineItemCollectionPresetDto>.Success(preset.ToDto());
    }

    public async Task<Result> DeleteLineItemCollectionPresetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var preset = await repository.GetLineItemCollectionPresetAsync(id, cancellationToken);
        if (preset is null)
            return Result.Failure("Line item collection preset not found.");
        repository.RemoveLineItemCollectionPreset(preset);
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static Result ValidateInvoice(InvoiceDetailsDto request)
    {
        if (string.IsNullOrWhiteSpace(request.InvoiceNumber))
            return Result.Failure("Invoice number is required.");
        if (!IsValidParty(request.Seller))
            return Result.Failure("Complete seller details are required.");
        if (!IsValidParty(request.Buyer))
            return Result.Failure("Complete buyer details are required.");
        if (request.LineItems is null || request.LineItems.Count == 0)
            return Result.Failure("At least one line item is required.");
        if (request.LineItems.Any(item => string.IsNullOrWhiteSpace(item.Description) || item.Quantity <= 0 || item.UnitPrice < 0))
            return Result.Failure("Line items require a description, a positive quantity, and a non-negative unit price.");
        return Result.Success();
    }

    private static Result ValidatePartyPreset(InvoicePartyPresetDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Label))
            return Result.Failure("Preset label is required.");
        if (!IsValidParty(request.Party))
            return Result.Failure("Complete party details are required.");
        return Result.Success();
    }

    private static Result ValidatePaymentTermsPreset(InvoicePaymentTermsPresetDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Label))
            return Result.Failure("Preset label is required.");
        if (string.IsNullOrWhiteSpace(request.Terms))
            return Result.Failure("Payment terms are required.");
        return Result.Success();
    }

    private static Result ValidateBankAccountPreset(InvoiceBankAccountPresetDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Label))
            return Result.Failure("Preset label is required.");
        if (request.BankDetails is null || new[] { request.BankDetails.Iban, request.BankDetails.Bic, request.BankDetails.Blz, request.BankDetails.AccountHolder }.All(string.IsNullOrWhiteSpace))
            return Result.Failure("At least one bank account field is required.");
        return Result.Success();
    }

    private static Result ValidateLineItemPreset(InvoiceLineItemPresetDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Label))
            return Result.Failure("Preset label is required.");
        return ValidatePresetLineItem(request.LineItem);
    }

    private static Result ValidateLineItemCollectionPreset(InvoiceLineItemCollectionPresetDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Label))
            return Result.Failure("Preset label is required.");
        if (request.LineItems is null || request.LineItems.Count == 0)
            return Result.Failure("At least one line item is required.");
        foreach (var item in request.LineItems)
        {
            var validation = ValidatePresetLineItem(item);
            if (!validation.IsSuccess)
                return validation;
        }
        return Result.Success();
    }

    private static Result ValidatePresetLineItem(InvoiceLineItemDto? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Description) || item.Quantity <= 0 || item.UnitPrice < 0)
            return Result.Failure("Line items require a description, a positive quantity, and a non-negative unit price.");
        return Result.Success();
    }

    private static bool IsValidParty(InvoicePartyDto? party)
    {
        return party is not null
            && !string.IsNullOrWhiteSpace(party.Name)
            && !string.IsNullOrWhiteSpace(party.Street)
            && !string.IsNullOrWhiteSpace(party.PostalCode)
            && !string.IsNullOrWhiteSpace(party.City);
    }

    private static void ApplyRequest(Invoice invoice, InvoiceDetailsDto request, DateTimeOffset now)
    {
        invoice.InvoiceNumber = request.InvoiceNumber.Trim();
        invoice.InvoiceDate = request.InvoiceDate;
        invoice.ServiceDate = request.ServiceDate;
        invoice.IntroText = request.IntroText?.Trim() ?? string.Empty;
        invoice.BodyText = request.BodyText?.Trim() ?? string.Empty;
        invoice.PaymentTerms = NullIfWhiteSpace(request.PaymentTerms);
        invoice.ClosingText = request.ClosingText?.Trim() ?? string.Empty;
        invoice.SignatureName = string.IsNullOrWhiteSpace(request.SignatureName)
            ? ClubConstants.Organization.LegalName
            : request.SignatureName.Trim();
        invoice.Greeting = request.Greeting?.Trim() ?? string.Empty;
        invoice.UpdatedAt = now;

        invoice.Parties.Clear();
        invoice.Parties.Add(CreateParty(invoice.Id, InvoicePartyRole.Seller, request.Seller));
        invoice.Parties.Add(CreateParty(invoice.Id, InvoicePartyRole.Buyer, request.Buyer));

        invoice.LineItems.Clear();
        for (var index = 0; index < request.LineItems.Count; index++)
        {
            var item = request.LineItems[index];
            invoice.LineItems.Add(new InvoiceLineItem
            {
                InvoiceId = invoice.Id,
                SortOrder = index,
                Description = item.Description.Trim(),
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity
            });
        }

        invoice.BankDetails ??= new InvoiceBankDetails { InvoiceId = invoice.Id };
        var bankDetails = request.BankDetails ?? new InvoiceBankDetailsDto();
        invoice.BankDetails.Iban = NullIfWhiteSpace(bankDetails.Iban);
        invoice.BankDetails.Bic = NullIfWhiteSpace(bankDetails.Bic);
        invoice.BankDetails.Blz = NullIfWhiteSpace(bankDetails.Blz);
        invoice.BankDetails.AccountHolder = NullIfWhiteSpace(bankDetails.AccountHolder);
    }

    private static InvoiceParty CreateParty(Guid invoiceId, InvoicePartyRole role, InvoicePartyDto request)
    {
        return new InvoiceParty
        {
            InvoiceId = invoiceId,
            Role = role,
            Name = request.Name.Trim(),
            Street = request.Street.Trim(),
            PostalCode = request.PostalCode.Trim(),
            City = request.City.Trim(),
            Country = NullIfWhiteSpace(request.Country)
        };
    }

    private static void ApplyPresetRequest(InvoicePartyPreset preset, InvoicePartyPresetDto request, DateTimeOffset now)
    {
        preset.Label = request.Label.Trim();
        preset.Name = request.Party.Name.Trim();
        preset.Street = request.Party.Street.Trim();
        preset.PostalCode = request.Party.PostalCode.Trim();
        preset.City = request.Party.City.Trim();
        preset.Country = NullIfWhiteSpace(request.Party.Country);
        preset.UpdatedAt = now;
    }

    private static void ApplyPaymentTermsPresetRequest(InvoicePaymentTermsPreset preset, InvoicePaymentTermsPresetDto request, DateTimeOffset now)
    {
        preset.Label = request.Label.Trim();
        preset.Terms = request.Terms.Trim();
        preset.UpdatedAt = now;
    }

    private static void ApplyBankAccountPresetRequest(InvoiceBankAccountPreset preset, InvoiceBankAccountPresetDto request, DateTimeOffset now)
    {
        preset.Label = request.Label.Trim();
        preset.Iban = NullIfWhiteSpace(request.BankDetails.Iban);
        preset.Bic = NullIfWhiteSpace(request.BankDetails.Bic);
        preset.Blz = NullIfWhiteSpace(request.BankDetails.Blz);
        preset.AccountHolder = NullIfWhiteSpace(request.BankDetails.AccountHolder);
        preset.UpdatedAt = now;
    }

    private static void ApplyLineItemPresetRequest(InvoiceLineItemPreset preset, InvoiceLineItemPresetDto request, DateTimeOffset now)
    {
        preset.Label = request.Label.Trim();
        preset.Description = request.LineItem.Description.Trim();
        preset.UnitPrice = request.LineItem.UnitPrice;
        preset.Quantity = request.LineItem.Quantity;
        preset.UpdatedAt = now;
    }

    private static void ApplyLineItemCollectionPresetRequest(InvoiceLineItemCollectionPreset preset, InvoiceLineItemCollectionPresetDto request, DateTimeOffset now)
    {
        preset.Label = request.Label.Trim();
        preset.UpdatedAt = now;
        preset.LineItems.Clear();
        for (var index = 0; index < request.LineItems.Count; index++)
        {
            var item = request.LineItems[index];
            preset.LineItems.Add(new InvoiceLineItemCollectionPresetItem
            {
                CollectionPresetId = preset.Id,
                SortOrder = index,
                Description = item.Description.Trim(),
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity
            });
        }
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
