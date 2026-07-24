using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Application.Mapping;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using AkGaming.Management.Modules.MemberManagement.Domain.Constants;
using ContractEnums = AkGaming.Management.Modules.MemberManagement.Contracts.Enums ; 
using DomainEnums = AkGaming.Management.Modules.MemberManagement.Domain.Enums;

namespace AkGaming.Management.Modules.MemberManagement.Application.Services;

public class MemberQueryService : IMemberQueryService {
    private readonly IMemberRepository _memberRepository;
    
    public MemberQueryService(IMemberRepository memberRepository) {
        _memberRepository = memberRepository;
    }
    
    /// <inheritdoc/>
    public async Task<Result<MemberDto>> GetMemberByGuidAsync(Guid id) {
        var memberResult = await _memberRepository.GetByMemberIdAsync(id);
        if (!memberResult.IsSuccess)
            return Result<MemberDto>.Failure(memberResult.Error ?? "Member not found");
        var member = memberResult.Value!;
        
        return Result<MemberDto>.Success(member.ToDto());
    }
    
    /// <inheritdoc/>
    public async Task<Result<MemberDto>> GetMemberByUserGuidAsync(Guid id) {
        var memberResult = await _memberRepository.GetByUserIdAsync(id);
        if (!memberResult.IsSuccess)
            return Result<MemberDto>.Failure(memberResult.Error ?? "Member not found");
        var member = memberResult.Value!;
        
        return Result<MemberDto>.Success(member.ToDto());
    }
    
    /// <inheritdoc/>
    public async Task<Result<ICollection<MemberDto>>> GetAllMembersAsync() {
        var membersResult = await _memberRepository.GetAllAsync();
        if (!membersResult.IsSuccess)
            return Result<ICollection<MemberDto>>.Failure(membersResult.Error ?? "Members not found");
        var members = membersResult.Value!;
        
        return Result<ICollection<MemberDto>>.Success(members.Select(m => m.ToDto()).ToList());
    }
    
    /// <inheritdoc/>
    public async Task<Result<ICollection<MemberDto>>> GetMembersWithStatusAsync(ContractEnums.MembershipStatus status) {
        var membersResult = await _memberRepository.GetAllAsync();
        if (!membersResult.IsSuccess)
            return Result<ICollection<MemberDto>>.Failure(membersResult.Error ?? "Members not found");
        var members = membersResult.Value!;
        
        return Result<ICollection<MemberDto>>.Success(members
            .Where(m => m.Status == (DomainEnums.MembershipStatus)status)
            .Select(m => m.ToDto()).ToList());
    }
    
    /// <inheritdoc/>
    public async Task<Result<ICollection<MemberDto>>> GetMembersWithStatusAsync(ICollection<ContractEnums.MembershipStatus> statuses) {
        var membersResult = await _memberRepository.GetAllAsync();
        if (!membersResult.IsSuccess)
            return Result<ICollection<MemberDto>>.Failure(membersResult.Error ?? "Members not found");
        var members = membersResult.Value!;
        
        return Result<ICollection<MemberDto>>.Success(members
            .Where(m => statuses.Contains((ContractEnums.MembershipStatus)m.Status))
            .Select(m => m.ToDto()).ToList());
    }

    /// <inheritdoc/>
    public async Task<Result<ICollection<TrialMemberDto>>> GetTrialMembersAsync() {
        var membersResult = await _memberRepository.GetAllAsync();
        if (!membersResult.IsSuccess)
            return Result<ICollection<TrialMemberDto>>.Failure(membersResult.Error ?? "Trial members not found");

        var trialMembers = membersResult.Value!
            .Where(member => member.Status == DomainEnums.MembershipStatus.InTrial)
            .Select(member => {
                var activeTrialStart = member.StatusChanges
                    .Where(change => change.NewStatus == DomainEnums.MembershipStatus.InTrial)
                    .OrderByDescending(change => change.Timestamp)
                    .FirstOrDefault();

                return new TrialMemberDto {
                    Member = member.ToDto(),
                    TrialStartedAt = activeTrialStart?.Timestamp,
                    TrialEndsAt = activeTrialStart?.Timestamp
                        .AddDays(MemberManagementConstants.DefaultTrialPeriodInDays)
                        .Date
                };
            })
            .OrderBy(trial => trial.TrialEndsAt is null)
            .ThenBy(trial => trial.TrialEndsAt)
            .ThenBy(trial => trial.Member.LastName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(trial => trial.Member.FirstName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result<ICollection<TrialMemberDto>>.Success(trialMembers);
    }
}
