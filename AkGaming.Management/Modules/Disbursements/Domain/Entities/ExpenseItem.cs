namespace AkGaming.Management.Modules.Disbursements.Domain.Entities;

public sealed class ExpenseItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReimbursementId { get; set; }
    public Reimbursement? Reimbursement { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly? IncurredOn { get; set; }
    public ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
}
