using System.Diagnostics;
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
    public async Task UpdateMembershipStatusAsync(Guid memberId, ContractEnums.MembershipStatus newStatus) {
        var member = await _members.GetByIdAsync(memberId);
        if (member is null)
            throw new NullReferenceException("Member not found");

        member.ChangeStatus((DomainEnums.MembershipStatus)newStatus);
        await _members.UpdateAsync(member);
        await _members.SaveChangesAsync();
    }
    
    /// <inheritdoc/>
    public async Task<DateTime?> GetDefaultEndOfTrialPeriodAsync(Guid memberId) {
        var member = await _members.GetByIdAsync(memberId);
        if (member is null)
            throw new NullReferenceException("Member not found");
        
        var statusChanges = member.StatusChanges;
        var startOfTrialPeriod = statusChanges
            .Where(sc => sc.NewStatus == DomainEnums.MembershipStatus.InTrial)
            .OrderBy(sc => sc.Timestamp)
            .FirstOrDefault();
        
        if (startOfTrialPeriod is null)
            Debug.WriteLine($"[MembershipUpdateService.GetDefaultEndOfTrialPeriodAsync] Error: Member '{member}' is not in trial period");
        
        return startOfTrialPeriod?.Timestamp.AddDays(MemberManagementConstants.DefaultTrialPeriodInDays);
    }
    
    /// <inheritdoc/>
    public async Task InsertMembershipStatusChangeEventAsync(Guid memberId, ContractEnums.MembershipStatus oldStatus, ContractEnums.MembershipStatus newStatus, DateTime timestamp) {
        var member = await _members.GetByIdAsync(memberId);
        if (member is null)
            throw new NullReferenceException("Member not found");
        
        member.StatusChanges.Add(new MembershipStatusChangeEvent {
            MemberId = memberId,
            OldStatus = (DomainEnums.MembershipStatus)oldStatus,
            NewStatus = (DomainEnums.MembershipStatus)newStatus,
            Timestamp = timestamp
        });
        
        await _members.UpdateAsync(member);
        await _members.SaveChangesAsync();
    }
}