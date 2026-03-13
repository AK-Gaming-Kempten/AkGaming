using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence.Repositories;

public class EfMembershipDueRepository : IMembershipDueRepository {
    private readonly MemberManagementDbContext _dbContext;

    public EfMembershipDueRepository(MemberManagementDbContext dbContext) {
        _dbContext = dbContext;
    }

    public async Task<Result<MembershipDue>> GetByIdAsync(int id) {
        try {
            var due = await _dbContext.MembershipDues
                .FirstOrDefaultAsync(x => x.Id == id);

            if (due is null)
                return Result<MembershipDue>.Failure("Membership due not found.");

            return Result<MembershipDue>.Success(due);
        }
        catch (Exception ex) {
            return Result<MembershipDue>.Failure($"Database error: {ex.Message}");
        }
    }

    public async Task<Result<List<MembershipDue>>> GetByPaymentPeriodIdAsync(int paymentPeriodId) {
        try {
            var dues = await _dbContext.MembershipDues
                .Where(x => x.PaymentPeriodId == paymentPeriodId)
                .OrderBy(x => x.DueDate)
                .ThenBy(x => x.MemberId)
                .ToListAsync();

            return Result<List<MembershipDue>>.Success(dues);
        }
        catch (Exception ex) {
            return Result<List<MembershipDue>>.Failure($"Database error: {ex.Message}");
        }
    }

    public async Task<Result<List<MembershipDue>>> GetByMemberIdAsync(Guid memberId) {
        try {
            var dues = await _dbContext.MembershipDues
                .Where(x => x.MemberId == memberId)
                .OrderByDescending(x => x.DueDate)
                .ToListAsync();

            return Result<List<MembershipDue>>.Success(dues);
        }
        catch (Exception ex) {
            return Result<List<MembershipDue>>.Failure($"Database error: {ex.Message}");
        }
    }

    public Result Add(MembershipDue due) {
        try {
            _dbContext.MembershipDues.Add(due);
            return Result.Success();
        }
        catch (Exception ex) {
            return Result.Failure($"Database error: {ex.Message}");
        }
    }

    public Result AddRange(ICollection<MembershipDue> dues) {
        try {
            _dbContext.MembershipDues.AddRange(dues);
            return Result.Success();
        }
        catch (Exception ex) {
            return Result.Failure($"Database error: {ex.Message}");
        }
    }

    public Result Update(MembershipDue due) {
        try {
            _dbContext.MembershipDues.Update(due);
            return Result.Success();
        }
        catch (Exception ex) {
            return Result.Failure($"Database error: {ex.Message}");
        }
    }

    public async Task<Result> SaveChangesAsync() {
        try {
            await _dbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex) {
            return Result.Failure($"Database error: {ex.Message}");
        }
    }
}
