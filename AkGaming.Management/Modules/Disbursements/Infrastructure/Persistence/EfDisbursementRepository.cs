using AkGaming.Management.Modules.Disbursements.Application.Interfaces;
using AkGaming.Management.Modules.Disbursements.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AkGaming.Management.Modules.Disbursements.Infrastructure.Persistence;

public sealed class EfDisbursementRepository(DisbursementsDbContext dbContext) : IDisbursementRepository
{
    public async Task<List<Reimbursement>> GetReimbursementsAsync(Guid? userId, CancellationToken cancellationToken)
    {
        var query = ReimbursementsQuery();
        if (userId.HasValue) query = query.Where(item => item.UserId == userId.Value);
        var items = await query.ToListAsync(cancellationToken);
        return items.OrderByDescending(item => item.CreatedAt).ToList();
    }

    public Task<Reimbursement?> GetReimbursementAsync(Guid id, CancellationToken cancellationToken) =>
        ReimbursementsQuery().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<Receipt?> GetReceiptAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Receipts.Include(item => item.ExpenseItem!).ThenInclude(item => item.Reimbursement).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<List<DisbursementEvent>> GetEventsAsync(CancellationToken cancellationToken)
    {
        var items = await EventsQuery().ToListAsync(cancellationToken);
        return items.OrderByDescending(item => item.OccurredOn).ThenBy(item => item.Name).ToList();
    }

    public Task<DisbursementEvent?> GetEventAsync(Guid id, CancellationToken cancellationToken) =>
        EventsQuery().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<Allocation?> GetAllocationAsync(Guid id, CancellationToken cancellationToken) =>
        AllocationsQuery().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<Allocation?> GetAllocationByTokenAsync(Guid token, CancellationToken cancellationToken) =>
        AllocationsQuery().FirstOrDefaultAsync(item => item.ShareToken == token, cancellationToken);

    public Task<AllocationApplication?> GetApplicationAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.AllocationApplications
            .Include(item => item.Allocation).ThenInclude(allocation => allocation!.Event)
            .Include(item => item.Approvals)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<List<Allocation>> GetAllocationsForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        AllocationsQuery().Where(item => item.Applications.Any(application => application.ApplicantUserId == userId || application.Approvals.Any(approval => approval.ApproverUserId == userId))).ToListAsync(cancellationToken);

    public async Task<bool> TryAddAllocationApplicationAsync(
        AllocationApplication application,
        decimal allocationAmount,
        int rejectedStatus,
        int cancelledStatus,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var committedAmount = await dbContext.AllocationApplications
                .Where(item => item.AllocationId == application.AllocationId
                    && item.Status != rejectedStatus
                    && item.Status != cancelledStatus)
                .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0m;
            if (committedAmount + application.Amount > allocationAmount)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            dbContext.Add(application);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }
    }

    public Task<bool> TryUpdateAllocationApplicationAsync(
        AllocationApplication application,
        decimal allocationAmount,
        int rejectedStatus,
        int cancelledStatus,
        CancellationToken cancellationToken)
    {
        return TrySaveAllocationApplicationAsync(application, application.Status, allocationAmount,
            rejectedStatus, cancelledStatus, cancellationToken);
    }

    public Task<bool> TryUpdateAllocationApplicationStatusAsync(
        AllocationApplication application,
        int newStatus,
        decimal allocationAmount,
        int rejectedStatus,
        int cancelledStatus,
        CancellationToken cancellationToken)
    {
        return TrySaveAllocationApplicationAsync(application, newStatus, allocationAmount,
            rejectedStatus, cancelledStatus, cancellationToken);
    }

    private async Task<bool> TrySaveAllocationApplicationAsync(
        AllocationApplication application,
        int newStatus,
        decimal allocationAmount,
        int rejectedStatus,
        int cancelledStatus,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            if (newStatus != rejectedStatus && newStatus != cancelledStatus)
            {
                var otherCommittedAmount = await dbContext.AllocationApplications
                    .Where(item => item.AllocationId == application.AllocationId
                        && item.Id != application.Id
                        && item.Status != rejectedStatus
                        && item.Status != cancelledStatus)
                    .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0m;
                if (otherCommittedAmount + application.Amount > allocationAmount)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }
            }

            application.Status = newStatus;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }
    }

    public void Add<T>(T entity) where T : class => dbContext.Add(entity);
    public void RemoveRange<T>(IEnumerable<T> entities) where T : class => dbContext.RemoveRange(entities);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<Reimbursement> ReimbursementsQuery() => dbContext.Reimbursements.Include(item => item.Expenses).ThenInclude(item => item.Receipts).AsSplitQuery();
    private IQueryable<DisbursementEvent> EventsQuery() => dbContext.DisbursementEvents.Include(item => item.Allocations).ThenInclude(item => item.Applications).ThenInclude(item => item.Approvals).AsSplitQuery();
    private IQueryable<Allocation> AllocationsQuery() => dbContext.Allocations.Include(item => item.Event).Include(item => item.Applications).ThenInclude(item => item.Approvals).AsSplitQuery();
}
