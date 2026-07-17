namespace AkGaming.Management.Modules.Disbursements.Contracts.DTO;

public sealed class ExpenseItemDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly? IncurredOn { get; set; }
    public List<ReceiptDto> Receipts { get; set; } = [];
}
