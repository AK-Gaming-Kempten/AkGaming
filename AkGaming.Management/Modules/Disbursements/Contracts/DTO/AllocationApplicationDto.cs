using AkGaming.Management.Modules.Disbursements.Contracts.Enums;

namespace AkGaming.Management.Modules.Disbursements.Contracts.DTO;

public sealed class AllocationApplicationDto
{
    public Guid Id { get; set; }
    public Guid AllocationId { get; set; }
    public Guid ApplicantUserId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public AllocationApplicationStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public PaymentMethodSnapshotDto PaymentMethod { get; set; } = new();
    public List<AllocationApprovalDto> Approvals { get; set; } = [];
}

public sealed class CreateAllocationApplicationRequest
{
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public Guid PaymentInformationId { get; set; }
}

public sealed class UpdateAllocationApplicationStatusRequest
{
    public AllocationApplicationStatus Status { get; set; }
}

public sealed class UpdateAllocationApplicationRequest
{
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public Guid? PaymentInformationId { get; set; }
}

public sealed class DiscordAllocationDecisionRequest
{
    public Guid UserId { get; set; }
    public string ApproverName { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
}
