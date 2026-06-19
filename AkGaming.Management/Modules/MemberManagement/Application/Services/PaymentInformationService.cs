using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using ContractType = AkGaming.Management.Modules.MemberManagement.Contracts.Enums.PaymentInformationType;
using DomainType = AkGaming.Management.Modules.MemberManagement.Domain.Enums.PaymentInformationType;

namespace AkGaming.Management.Modules.MemberManagement.Application.Services;

public class PaymentInformationService(IMemberRepository memberRepository, IPaymentInformationRepository paymentInformationRepository)
    : IPaymentInformationService {
    public async Task<Result<ICollection<PaymentInformationDto>>> GetForUserAsync(Guid userId) {
        var memberResult = await memberRepository.GetByUserIdAsync(userId);
        if (!memberResult.IsSuccess)
            return Result<ICollection<PaymentInformationDto>>.Failure(memberResult.Error ?? "Profile not found.");

        var result = await paymentInformationRepository.GetByMemberIdAsync(memberResult.Value!.Id);
        return result.IsSuccess
            ? Result<ICollection<PaymentInformationDto>>.Success(result.Value!.Select(ToDto).ToList())
            : Result<ICollection<PaymentInformationDto>>.Failure(result.Error ?? "Payment information could not be loaded.");
    }

    public async Task<Result<PaymentInformationDto>> CreateAsync(Guid userId, PaymentInformationDto paymentInformation) {
        var validationResult = Validate(paymentInformation);
        if (!validationResult.IsSuccess)
            return Result<PaymentInformationDto>.Failure(validationResult.Error!);

        var memberResult = await memberRepository.GetByUserIdAsync(userId);
        if (!memberResult.IsSuccess)
            return Result<PaymentInformationDto>.Failure(memberResult.Error ?? "Profile not found.");

        var entity = new PaymentInformation { MemberId = memberResult.Value!.Id };
        Apply(paymentInformation, entity);
        paymentInformationRepository.Add(entity);
        var saveResult = await paymentInformationRepository.SaveChangesAsync();
        return saveResult.IsSuccess
            ? Result<PaymentInformationDto>.Success(ToDto(entity))
            : Result<PaymentInformationDto>.Failure(saveResult.Error!);
    }

    public async Task<Result> UpdateAsync(Guid userId, Guid id, PaymentInformationDto paymentInformation) {
        var validationResult = Validate(paymentInformation);
        if (!validationResult.IsSuccess)
            return validationResult;

        var ownedResult = await GetOwnedAsync(userId, id);
        if (!ownedResult.IsSuccess)
            return Result.Failure(ownedResult.Error!);

        Apply(paymentInformation, ownedResult.Value!);
        return await paymentInformationRepository.SaveChangesAsync();
    }

    public async Task<Result> DeleteAsync(Guid userId, Guid id) {
        var ownedResult = await GetOwnedAsync(userId, id);
        if (!ownedResult.IsSuccess)
            return Result.Failure(ownedResult.Error!);

        paymentInformationRepository.Delete(ownedResult.Value!);
        return await paymentInformationRepository.SaveChangesAsync();
    }

    private async Task<Result<PaymentInformation>> GetOwnedAsync(Guid userId, Guid id) {
        var memberResult = await memberRepository.GetByUserIdAsync(userId);
        if (!memberResult.IsSuccess)
            return Result<PaymentInformation>.Failure(memberResult.Error ?? "Profile not found.");
        var itemResult = await paymentInformationRepository.GetByIdAsync(id);
        if (!itemResult.IsSuccess || itemResult.Value!.MemberId != memberResult.Value!.Id)
            return Result<PaymentInformation>.Failure("Payment information not found.");
        return itemResult;
    }

    private static Result Validate(PaymentInformationDto item) {
        if (item.Type == ContractType.PayPal && string.IsNullOrWhiteSpace(item.PayPalEmail))
            return Result.Failure("PayPal email is required.");
        if (item.Type == ContractType.BankAccount && (string.IsNullOrWhiteSpace(item.AccountHolder) || string.IsNullOrWhiteSpace(item.Iban)))
            return Result.Failure("Account holder and IBAN are required.");
        return Result.Success();
    }

    private static void Apply(PaymentInformationDto source, PaymentInformation target) {
        target.Type = (DomainType)source.Type;
        target.PayPalEmail = source.Type == ContractType.PayPal ? source.PayPalEmail?.Trim() : null;
        target.AccountHolder = source.Type == ContractType.BankAccount ? source.AccountHolder?.Trim() : null;
        target.Iban = source.Type == ContractType.BankAccount ? source.Iban?.Replace(" ", string.Empty).ToUpperInvariant() : null;
        target.Bic = source.Type == ContractType.BankAccount ? source.Bic?.Replace(" ", string.Empty).ToUpperInvariant() : null;
    }

    private static PaymentInformationDto ToDto(PaymentInformation item) => new() {
        Id = item.Id,
        Type = (ContractType)item.Type,
        PayPalEmail = item.PayPalEmail,
        AccountHolder = item.AccountHolder,
        Iban = item.Iban,
        Bic = item.Bic
    };
}
