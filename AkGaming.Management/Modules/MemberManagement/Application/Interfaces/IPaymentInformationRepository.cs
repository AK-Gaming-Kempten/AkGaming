using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;

namespace AkGaming.Management.Modules.MemberManagement.Application.Interfaces;

public interface IPaymentInformationRepository {
    Task<Result<List<PaymentInformation>>> GetByMemberIdAsync(Guid memberId);
    Task<Result<PaymentInformation>> GetByIdAsync(Guid id);
    Result Add(PaymentInformation paymentInformation);
    Result Delete(PaymentInformation paymentInformation);
    Task<Result> SaveChangesAsync();
}
