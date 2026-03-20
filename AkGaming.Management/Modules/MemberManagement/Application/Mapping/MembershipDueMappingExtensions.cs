using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using ContractEnums = AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using DomainEnums = AkGaming.Management.Modules.MemberManagement.Domain.Enums;

namespace AkGaming.Management.Modules.MemberManagement.Application.Mapping;

public static class MembershipDueMappingExtensions {
    public static MembershipDueDto ToDto(this MembershipDue due) => new() {
        Id = due.Id,
        PaymentPeriodId = due.PaymentPeriodId,
        MemberId = due.MemberId,
        Status = (ContractEnums.MembershipDueStatus)due.Status,
        DueAmount = due.DueAmount,
        PaidAmount = due.PaidAmount,
        DueDate = due.DueDate,
        SettledAt = due.SettledAt,
        SettlementReference = due.SettlementReference
    };

    public static MembershipPaymentPeriodDto ToDto(this MembershipPaymentPeriod paymentPeriod) => new() {
        Id = paymentPeriod.Id,
        Name = paymentPeriod.Name,
        DueDate = paymentPeriod.DueDate,
        DefaultDueAmount = paymentPeriod.DefaultDueAmount,
        ReducedDueAmount = paymentPeriod.ReducedDueAmount,
        CreatedAt = paymentPeriod.CreatedAt
    };

    public static MembershipDue ToMembershipDue(this MembershipDueDto due) => new() {
        Id = due.Id,
        PaymentPeriodId = due.PaymentPeriodId,
        MemberId = due.MemberId,
        Status = (DomainEnums.MembershipDueStatus)due.Status,
        DueAmount = due.DueAmount,
        PaidAmount = due.PaidAmount,
        DueDate = due.DueDate,
        SettledAt = due.SettledAt,
        SettlementReference = due.SettlementReference
    };

    public static MembershipPaymentPeriod ToMembershipPaymentPeriod(this MembershipPaymentPeriodCreateDto request) => new() {
        Name = request.Name,
        DueDate = request.DueDate,
        DefaultDueAmount = request.DefaultDueAmount,
        ReducedDueAmount = request.ReducedDueAmount,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
