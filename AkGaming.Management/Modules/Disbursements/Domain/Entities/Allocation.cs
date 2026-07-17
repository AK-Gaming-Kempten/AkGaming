namespace AkGaming.Management.Modules.Disbursements.Domain.Entities;

public sealed class Allocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public DisbursementEvent? Event { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public Guid ShareToken { get; set; } = Guid.NewGuid();
    public ICollection<AllocationApplication> Applications { get; set; } = new List<AllocationApplication>();
}
