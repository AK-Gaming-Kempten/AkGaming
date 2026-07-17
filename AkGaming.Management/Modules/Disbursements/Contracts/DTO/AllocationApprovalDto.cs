namespace AkGaming.Management.Modules.Disbursements.Contracts.DTO;

public sealed class AllocationApprovalDto
{
    public Guid Id { get; set; }
    public Guid ApproverUserId { get; set; }
    public string ApproverName { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class DecideAllocationApplicationRequest
{
    public bool IsApproved { get; set; }
}
