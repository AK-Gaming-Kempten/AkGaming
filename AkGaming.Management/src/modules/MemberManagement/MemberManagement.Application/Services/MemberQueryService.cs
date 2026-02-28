using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using MemberManagement.Contracts.DTO;
using MemberManagement.Application.Mapping;
using MemberManagement.Contracts.Services;
using ContractEnums = MemberManagement.Contracts.Enums ; 
using DomainEnums = MemberManagement.Domain.Enums;

namespace MemberManagement.Application.Services;

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
}