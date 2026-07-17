namespace AkGaming.Management.Modules.Disbursements.Domain.Entities;

public sealed class AllocationApproval
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }
    public AllocationApplication? Application { get; set; }
    public Guid ApproverUserId { get; set; }
    public string ApproverName { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
