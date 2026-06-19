using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence.Repositories;

public class EfPaymentInformationRepository(MemberManagementDbContext dbContext) : IPaymentInformationRepository {
    public async Task<Result<List<PaymentInformation>>> GetByMemberIdAsync(Guid memberId) {
        var items = await dbContext.PaymentInformation.Where(x => x.MemberId == memberId).ToListAsync();
        return Result<List<PaymentInformation>>.Success(items);
    }

    public async Task<Result<PaymentInformation>> GetByIdAsync(Guid id) {
        var item = await dbContext.PaymentInformation.FirstOrDefaultAsync(x => x.Id == id);
        return item is null
            ? Result<PaymentInformation>.Failure("Payment information not found.")
            : Result<PaymentInformation>.Success(item);
    }

    public Result Add(PaymentInformation paymentInformation) {
        dbContext.PaymentInformation.Add(paymentInformation);
        return Result.Success();
    }

    public Result Delete(PaymentInformation paymentInformation) {
        dbContext.PaymentInformation.Remove(paymentInformation);
        return Result.Success();
    }

    public async Task<Result> SaveChangesAsync() {
        try {
            await dbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception exception) {
            return Result.Failure($"Payment information could not be saved: {exception.Message}");
        }
    }
}
