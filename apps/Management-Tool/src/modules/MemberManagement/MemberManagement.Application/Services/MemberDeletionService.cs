using AKG.Common.Extensions;
using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using MemberManagement.Contracts.Services;

namespace MemberManagement.Application.Services;

public class MemberDeletionService : IMemberDeletionService {
    private readonly IMemberRepository _memberRepository;
    
    public MemberDeletionService(IMemberRepository memberRepository) {
        _memberRepository = memberRepository;
    }
    
    /// <inheritdoc/>
    public async Task<Result> DeleteMemberAsync(Guid memberId) {
        var deleteResult = await _memberRepository.TryDelete(memberId)
            .Then(() => _memberRepository.SaveChangesAsync());
        
        return deleteResult;
    }
}