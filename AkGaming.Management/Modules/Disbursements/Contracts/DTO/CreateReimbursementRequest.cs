namespace AkGaming.Management.Modules.Disbursements.Contracts.DTO;

public sealed class CreateReimbursementRequest
{
    public string Purpose { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid PaymentInformationId { get; set; }
    public List<CreateExpenseItemRequest> Expenses { get; set; } = [];
}

public sealed class CreateExpenseItemRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly? IncurredOn { get; set; }
    public List<int> ReceiptIndexes { get; set; } = [];
}
