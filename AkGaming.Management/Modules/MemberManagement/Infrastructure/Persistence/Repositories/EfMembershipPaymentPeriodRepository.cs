using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence.Repositories;

public class EfMembershipPaymentPeriodRepository : IMembershipPaymentPeriodRepository {
    private readonly MemberManagementDbContext _dbContext;

    public EfMembershipPaymentPeriodRepository(MemberManagementDbContext dbContext) {
        _dbContext = dbContext;
    }

    public async Task<Result<MembershipPaymentPeriod>> GetByIdAsync(int id) {
        try {
            var paymentPeriod = await _dbContext.MembershipPaymentPeriods
                .FirstOrDefaultAsync(x => x.Id == id);

            if (paymentPeriod is null)
                return Result<MembershipPaymentPeriod>.Failure("Payment period not found.");

            return Result<MembershipPaymentPeriod>.Success(paymentPeriod);
        }
        catch (Exception ex) {
            return Result<MembershipPaymentPeriod>.Failure($"Database error: {ex.Message}");
        }
    }

    public async Task<Result<MembershipPaymentPeriod>> GetCurrentAsync() {
        try {
            var paymentPeriod = await _dbContext.MembershipPaymentPeriods
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (paymentPeriod is null)
                return Result<MembershipPaymentPeriod>.Failure("No payment period exists.");

            return Result<MembershipPaymentPeriod>.Success(paymentPeriod);
        }
        catch (Exception ex) {
            return Result<MembershipPaymentPeriod>.Failure($"Database error: {ex.Message}");
        }
    }

    public Result Add(MembershipPaymentPeriod paymentPeriod) {
        try {
            _dbContext.MembershipPaymentPeriods.Add(paymentPeriod);
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
