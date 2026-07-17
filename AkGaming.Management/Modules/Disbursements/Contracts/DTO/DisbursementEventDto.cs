namespace AkGaming.Management.Modules.Disbursements.Contracts.DTO;

public sealed class DisbursementEventDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? OccurredOn { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<AllocationDto> Allocations { get; set; } = [];
}

public sealed class SaveDisbursementEventRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? OccurredOn { get; set; }
}
