namespace AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;

public sealed record InvoiceSummaryDto(
    Guid Id,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    string BuyerName,
    decimal TotalAmount,
    DateTimeOffset UpdatedAt);
