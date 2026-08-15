using AkGaming.Management.Modules.Disbursements.Contracts.Enums;
using AkGaming.Management.Modules.Disbursements.Domain.Entities;

namespace AkGaming.Management.Modules.Disbursements.Application.Interfaces;

public interface IDisbursementNotificationOutbox
{
    void EnqueueSubmitted(Reimbursement reimbursement);
    void EnqueueStatusChanged(Reimbursement reimbursement, DisbursementStatus previousStatus);
    void EnqueueAllocationAvailable(Allocation allocation);
    void EnqueueAllocationClaimChanged(AllocationApplication application);
    void EnqueueAllocationClaimChanged(AllocationApplication application, bool startsNewReview);
}
