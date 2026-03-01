using AkGaming.Core.Common.Extensions;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;

namespace AkGaming.Management.Modules.MemberManagement.Application.Services;

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