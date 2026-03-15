using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Application.Mapping;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using AkGaming.Management.Modules.MemberManagement.Domain.Constants;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using DomainEnums = AkGaming.Management.Modules.MemberManagement.Domain.Enums;

namespace AkGaming.Management.Modules.MemberManagement.Application.Services;

public class MembershipDueService : IMembershipDueService {
    private readonly IMembershipDueRepository _dueRepository;
    private readonly IMembershipPaymentPeriodRepository _paymentPeriodRepository;
    private readonly IMemberRepository _memberRepository;

    public MembershipDueService(
        IMembershipDueRepository dueRepository,
        IMembershipPaymentPeriodRepository paymentPeriodRepository,
        IMemberRepository memberRepository) {
        _dueRepository = dueRepository;
        _paymentPeriodRepository = paymentPeriodRepository;
        _memberRepository = memberRepository;
    }

    /// <inheritdoc />
    public async Task<Result<MembershipPaymentPeriodDto>> CreatePaymentPeriodAsync(MembershipPaymentPeriodCreateDto request, Guid? performedByUserId = null) {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<MembershipPaymentPeriodDto>.Failure("Payment period name is required.");

        var membersResult = await _memberRepository.GetAllAsync();
        if (!membersResult.IsSuccess)
            return Result<MembershipPaymentPeriodDto>.Failure(membersResult.Error ?? "Members could not be loaded.");
        var members = membersResult.Value!;

        var paymentPeriod = request.ToMembershipPaymentPeriod();
        var addPaymentPeriodResult = _paymentPeriodRepository.Add(paymentPeriod);
        if (!addPaymentPeriodResult.IsSuccess)
            return Result<MembershipPaymentPeriodDto>.Failure(addPaymentPeriodResult.Error ?? "Payment period could not be created.");

        var activeMembers = members.Where(member => QualifiesForDue(member, paymentPeriod)).ToList();
        var dues = activeMembers.Select(member => new MembershipDue {
            MemberId = member.Id,
            PaymentPeriod = paymentPeriod,
            Status = DomainEnums.MembershipDueStatus.Pending,
            DueAmount = request.DefaultDueAmount,
            PaidAmount = null,
            DueDate = request.DueDate,
            SettledAt = null,
            SettlementReference = null
        }).ToList();

        if (dues.Count > 0) {
            var addDuesResult = _dueRepository.AddRange(dues);
            if (!addDuesResult.IsSuccess)
                return Result<MembershipPaymentPeriodDto>.Failure(addDuesResult.Error ?? "Membership dues could not be created.");
        }

        var saveResult = await _dueRepository.SaveChangesAsync();
        if (!saveResult.IsSuccess)
            return Result<MembershipPaymentPeriodDto>.Failure(saveResult.Error ?? "Changes could not be saved.");

        return Result<MembershipPaymentPeriodDto>.Success(paymentPeriod.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<ICollection<MembershipPaymentPeriodDto>>> GetPaymentPeriodsAsync() {
        var paymentPeriodsResult = await _paymentPeriodRepository.GetAllAsync();
        if (!paymentPeriodsResult.IsSuccess)
            return Result<ICollection<MembershipPaymentPeriodDto>>.Failure(paymentPeriodsResult.Error ?? "Payment periods could not be loaded.");
        var paymentPeriods = paymentPeriodsResult.Value!;

        return Result<ICollection<MembershipPaymentPeriodDto>>.Success(paymentPeriods.Select(x => x.ToDto()).ToList());
    }

    /// <inheritdoc />
    public async Task<Result<ICollection<MembershipDueDto>>> GetCurrentPaymentPeriodDuesAsync() {
        var currentPeriodResult = await _paymentPeriodRepository.GetCurrentAsync();
        if (!currentPeriodResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(currentPeriodResult.Error ?? "Current payment period not found.");
        var currentPeriod = currentPeriodResult.Value!;

        var duesResult = await _dueRepository.GetByPaymentPeriodIdAsync(currentPeriod.Id);
        if (!duesResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(duesResult.Error ?? "Dues not found.");
        var dues = duesResult.Value!;

        return Result<ICollection<MembershipDueDto>>.Success(dues.Select(d => d.ToDto()).ToList());
    }

    /// <inheritdoc />
    public async Task<Result<ICollection<MembershipDueDto>>> GetPaymentPeriodDuesAsync(int paymentPeriodId) {
        var duesResult = await _dueRepository.GetByPaymentPeriodIdAsync(paymentPeriodId);
        if (!duesResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(duesResult.Error ?? "Dues not found.");
        var dues = duesResult.Value!;

        return Result<ICollection<MembershipDueDto>>.Success(dues.Select(d => d.ToDto()).ToList());
    }

    /// <inheritdoc />
    public async Task<Result<ICollection<MembershipDueDto>>> AddMembersToPaymentPeriodAsync(int paymentPeriodId, ICollection<Guid> memberIds, Guid? performedByUserId = null) {
        if (memberIds.Count == 0)
            return Result<ICollection<MembershipDueDto>>.Failure("At least one member id must be provided.");

        var paymentPeriodResult = await _paymentPeriodRepository.GetByIdAsync(paymentPeriodId);
        if (!paymentPeriodResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(paymentPeriodResult.Error ?? "Payment period not found.");
        var paymentPeriod = paymentPeriodResult.Value!;

        var duesForPeriodResult = await _dueRepository.GetByPaymentPeriodIdAsync(paymentPeriodId);
        if (!duesForPeriodResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(duesForPeriodResult.Error ?? "Dues could not be loaded.");
        var duesForPeriod = duesForPeriodResult.Value!;
        var existingMemberIds = duesForPeriod.Select(d => d.MemberId).ToHashSet();

        var requestedMemberIds = memberIds.Distinct().Where(id => !existingMemberIds.Contains(id)).ToList();
        if (requestedMemberIds.Count == 0)
            return Result<ICollection<MembershipDueDto>>.Success(duesForPeriod.Select(d => d.ToDto()).ToList());

        var membersResult = await _memberRepository.GetAllAsync();
        if (!membersResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(membersResult.Error ?? "Members could not be loaded.");
        var members = membersResult.Value!;
        var validMembers = members.Where(m => requestedMemberIds.Contains(m.Id)).ToList();

        if (validMembers.Count == 0)
            return Result<ICollection<MembershipDueDto>>.Failure("No valid members were provided.");

        var duesToAdd = validMembers.Select(member => new MembershipDue {
            MemberId = member.Id,
            PaymentPeriodId = paymentPeriod.Id,
            Status = DomainEnums.MembershipDueStatus.Pending,
            DueAmount = paymentPeriod.DefaultDueAmount,
            PaidAmount = null,
            DueDate = paymentPeriod.DueDate,
            SettledAt = null,
            SettlementReference = null
        }).ToList();

        var addResult = _dueRepository.AddRange(duesToAdd);
        if (!addResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(addResult.Error ?? "New dues could not be added.");

        var saveResult = await _dueRepository.SaveChangesAsync();
        if (!saveResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(saveResult.Error ?? "Changes could not be saved.");

        var updatedDues = duesForPeriod.Concat(duesToAdd).Select(d => d.ToDto()).ToList();
        return Result<ICollection<MembershipDueDto>>.Success(updatedDues);
    }

    /// <inheritdoc />
    public async Task<Result<ICollection<MembershipDueDto>>> GetDuesForMemberAsync(Guid memberId) {
        var duesResult = await _dueRepository.GetByMemberIdAsync(memberId);
        if (!duesResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(duesResult.Error ?? "Dues not found.");
        var dues = duesResult.Value!;

        return Result<ICollection<MembershipDueDto>>.Success(dues.Select(d => d.ToDto()).ToList());
    }
    
    /// <inheritdoc />
    public async Task<Result> UpdateDueAsync(int dueId, MembershipDueDto due, Guid? performedByUserId = null) {
        var dueResult = await _dueRepository.GetByIdAsync(dueId);
        if (!dueResult.IsSuccess)
            return dueResult;
        var existingDue = dueResult.Value!;

        existingDue.Status = (DomainEnums.MembershipDueStatus)due.Status;
        existingDue.DueAmount = due.DueAmount;
        existingDue.PaidAmount = due.PaidAmount;
        existingDue.DueDate = due.DueDate;
        existingDue.SettledAt = due.SettledAt;
        existingDue.SettlementReference = due.SettlementReference;

        var updateResult = _dueRepository.Update(existingDue);
        if (!updateResult.IsSuccess)
            return updateResult;

        return await _dueRepository.SaveChangesAsync();
    }

    private static bool QualifiesForDue(Member member, MembershipPaymentPeriod paymentPeriod) {
        if (member.Status == DomainEnums.MembershipStatus.Member)
            return true;

        if (member.Status != DomainEnums.MembershipStatus.InTrial)
            return false;

        var inTrialStart = member.StatusChanges
            .Where(sc => sc.NewStatus == DomainEnums.MembershipStatus.InTrial)
            .OrderBy(sc => sc.Timestamp)
            .FirstOrDefault();

        if (inTrialStart is null)
            return false;

        var trialEndDate = DateOnly.FromDateTime(inTrialStart.Timestamp.AddDays(MemberManagementConstants.DefaultTrialPeriodInDays));
        return trialEndDate >= paymentPeriod.DueDate && trialEndDate <= paymentPeriod.DueDate.AddMonths(3);
    }
}
