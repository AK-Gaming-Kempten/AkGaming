using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

namespace AkGaming.Management.Modules.MemberManagement.Contracts.Services;

public interface IPaymentInformationService {
    Task<Result<ICollection<PaymentInformationDto>>> GetForUserAsync(Guid userId);
    Task<Result<PaymentInformationDto>> CreateAsync(Guid userId, PaymentInformationDto paymentInformation);
    Task<Result> UpdateAsync(Guid userId, Guid id, PaymentInformationDto paymentInformation);
    Task<Result> DeleteAsync(Guid userId, Guid id);
}
