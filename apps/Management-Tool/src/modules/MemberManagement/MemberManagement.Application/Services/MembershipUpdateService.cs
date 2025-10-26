using AKG.Common.Extensions;
using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using Membermanagement.Contracts.Services;
using MemberManagement.Domain.Constants;
using MemberManagement.Domain.Entities;
using ContractEnums = Membermanagement.Contracts.Enums ; 
using DomainEnums = MemberManagement.Domain.Enums;

namespace MemberManagement.Application.Services;

public class MembershipUpdateService : IMembershipUpdateService {
    private readonly IMemberRepository _members;

    public MembershipUpdateService(IMemberRepository members) {
        _members = members;
    }
    
    /// <inheritdoc/>
    public async Task<Result> UpdateMembershipStatusAsync(Guid memberId, ContractEnums.MembershipStatus newStatus) {
        var memberResult = await _members.GetByMemberIdAsync(memberId);
        if (!memberResult.IsSuccess)
            return memberResult;
        var member = memberResult.Value!;

        var result = await member.ChangeStatus((DomainEnums.MembershipStatus)newStatus)
            .Then(() => _members.UpdateAsync(member))
            .Then(() => _members.SaveChangesAsync());
        
        return result;
    }
    
    /// <inheritdoc/>
    public async Task<Result<DateTime?>> GetDefaultEndOfTrialPeriodAsync(Guid memberId) {
        var memberResult = await _members.GetByMemberIdAsync(memberId);
        if (!memberResult.IsSuccess)
            return Result<DateTime?>.Failure("Error: Member not found");
        var member = memberResult.Value!;
        
        var statusChanges = member.StatusChanges;
        var startOfTrialPeriod = statusChanges
            .Where(sc => sc.NewStatus == DomainEnums.MembershipStatus.InTrial)
            .OrderBy(sc => sc.Timestamp)
            .FirstOrDefault();
        
        if (startOfTrialPeriod is null)
            return Result<DateTime?>.Failure($"Error: Member '{member}' did not start their trial period");
        
        return Result<DateTime?>.Success(startOfTrialPeriod?.Timestamp.AddDays(MemberManagementConstants.DefaultTrialPeriodInDays).Date);
    }
    
    /// <inheritdoc/>
    public async Task<Result> InsertMembershipStatusChangeEventAsync(Guid memberId, ContractEnums.MembershipStatus oldStatus, ContractEnums.MembershipStatus newStatus, DateTime timestamp) {
        var memberResult = await _members.GetByMemberIdAsync(memberId);
        if (!memberResult.IsSuccess)
            return memberResult;
        var member = memberResult.Value!;
        
        member.StatusChanges.Add(new MembershipStatusChangeEvent {
            MemberId = memberId,
            OldStatus = (DomainEnums.MembershipStatus)oldStatus,
            NewStatus = (DomainEnums.MembershipStatus)newStatus,
            Timestamp = timestamp
        });
        
        var result = await _members.UpdateAsync(member)
            .Then(() => _members.SaveChangesAsync());
        
        return result;
    }
}