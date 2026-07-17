using AkGaming.Management.Modules.Disbursements.Contracts.Enums;

namespace AkGaming.Management.Modules.Disbursements.Contracts.DTO;

public sealed class ReimbursementDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? AdministrativeNote { get; set; }
    public DisbursementStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public decimal TotalAmount => Expenses.Sum(expense => expense.Amount);
    public PaymentMethodSnapshotDto PaymentMethod { get; set; } = new();
    public List<ExpenseItemDto> Expenses { get; set; } = [];
}
