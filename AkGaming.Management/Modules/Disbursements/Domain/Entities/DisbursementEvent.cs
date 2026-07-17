namespace AkGaming.Management.Modules.Disbursements.Domain.Entities;

public sealed class DisbursementEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? OccurredOn { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
}
