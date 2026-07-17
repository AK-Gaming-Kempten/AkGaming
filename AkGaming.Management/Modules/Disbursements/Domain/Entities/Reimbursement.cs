namespace AkGaming.Management.Modules.Disbursements.Domain.Entities;

public sealed class Reimbursement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? AdministrativeNote { get; set; }
    public int Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public PaymentMethodSnapshot PaymentMethod { get; set; } = new();
    public ICollection<ExpenseItem> Expenses { get; set; } = new List<ExpenseItem>();
}
