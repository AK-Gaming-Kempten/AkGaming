using AkGaming.Core.Common.Extensions;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Application.Mapping;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using AkGaming.Management.Modules.MemberManagement.Domain.Constants;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using ContractEnums = AkGaming.Management.Modules.MemberManagement.Contracts.Enums ; 
using DomainEnums = AkGaming.Management.Modules.MemberManagement.Domain.Enums;

namespace AkGaming.Management.Modules.MemberManagement.Application.Services;

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
    public async Task<Result> InsertMembershipStatusChangeEventAsync(Guid memberId, MembershipStatusChangeEventDto changeEvent) {
        var memberResult = await _members.GetByMemberIdAsync(memberId);
        if (!memberResult.IsSuccess)
            return memberResult;
        var member = memberResult.Value!;
        
        if(member.StatusChanges.All( x => x.Timestamp < changeEvent.Timestamp))
            member.Status = (DomainEnums.MembershipStatus)changeEvent.NewStatus;
        member.StatusChanges.Add(changeEvent.ToMembershipStatusChangeEvent());
        
        var result = await _members.SaveChangesAsync();
        
        return result;
    }
    
    /// <inheritdoc/>
    public async Task<Result<List<MembershipStatusChangeEventDto>>> GetMembershipStatusChangesAsync(Guid memberId) {
        var memberResult = await _members.GetByMemberIdAsync(memberId);
        if (!memberResult.IsSuccess)
            return Result<List<MembershipStatusChangeEventDto>>.Failure("Error: Member not found");
        var member = memberResult.Value!;
        
        return Result<List<MembershipStatusChangeEventDto>>.Success(member.StatusChanges.Select(sc => sc.ToDto()).ToList());
    }
}