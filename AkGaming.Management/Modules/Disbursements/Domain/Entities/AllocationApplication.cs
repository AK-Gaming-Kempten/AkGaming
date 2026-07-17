namespace AkGaming.Management.Modules.Disbursements.Domain.Entities;

public sealed class AllocationApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AllocationId { get; set; }
    public Allocation? Allocation { get; set; }
    public Guid ApplicantUserId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public int Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public PaymentMethodSnapshot PaymentMethod { get; set; } = new();
    public ICollection<AllocationApproval> Approvals { get; set; } = new List<AllocationApproval>();
}
