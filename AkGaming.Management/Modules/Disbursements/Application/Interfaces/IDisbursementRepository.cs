using AkGaming.Management.Modules.Disbursements.Domain.Entities;

namespace AkGaming.Management.Modules.Disbursements.Application.Interfaces;

public interface IDisbursementRepository
{
    Task<List<Reimbursement>> GetReimbursementsAsync(Guid? userId, CancellationToken cancellationToken);
    Task<Reimbursement?> GetReimbursementAsync(Guid id, CancellationToken cancellationToken);
    Task<Receipt?> GetReceiptAsync(Guid id, CancellationToken cancellationToken);
    Task<List<DisbursementEvent>> GetEventsAsync(CancellationToken cancellationToken);
    Task<DisbursementEvent?> GetEventAsync(Guid id, CancellationToken cancellationToken);
    Task<Allocation?> GetAllocationAsync(Guid id, CancellationToken cancellationToken);
    Task<Allocation?> GetAllocationByTokenAsync(Guid token, CancellationToken cancellationToken);
    Task<AllocationApplication?> GetApplicationAsync(Guid id, CancellationToken cancellationToken);
    Task<List<Allocation>> GetAllocationsForUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> TryAddAllocationApplicationAsync(AllocationApplication application, decimal allocationAmount, int rejectedStatus, CancellationToken cancellationToken);
    Task<bool> TryUpdateAllocationApplicationStatusAsync(AllocationApplication application, int newStatus, decimal allocationAmount, int rejectedStatus, CancellationToken cancellationToken);
    void Add<T>(T entity) where T : class;
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
