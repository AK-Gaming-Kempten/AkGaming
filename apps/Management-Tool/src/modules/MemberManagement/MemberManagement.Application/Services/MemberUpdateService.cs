using AKG.Common.Extensions;
using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using MemberManagement.Application.Mapping;
using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Services;
using MemberManagement.Domain.ValueObjects;

namespace MemberManagement.Application.Services;

public class MemberUpdateService : IMemberUpdateService {
    
    private readonly IMemberRepository _memberRepository;
    public MemberUpdateService(IMemberRepository memberRepository) {
        _memberRepository = memberRepository;
    }
    
    /// <inheritdoc/>
    public async Task<Result> UpdateMemberAsync(Guid memberId, MemberDto memberData) {
        var memberResult = await _memberRepository.GetByMemberIdAsync(memberId);
        if (!memberResult.IsSuccess)
            return memberResult;
        var member = memberResult.Value!;

        member = memberData.ToMember();

        var result = await _memberRepository.Update(member)
            .Then(() => _memberRepository.SaveChangesAsync());
        
        return result;
    }
}