using AKG.Common.Extensions;
using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using MemberManagement.Application.Mapping;
using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Services;
using MemberManagement.Domain.Entities;
using MemberManagement.Domain.ValueObjects;
using UserManagement.Contracts.Services;

namespace MemberManagement.Application.Services;

public class MemberCreationService : IMemberCreationService {
    private readonly IMemberRepository _memberRepository;
    
    public MemberCreationService(IMemberRepository memberRepository) {
        _memberRepository = memberRepository;
    }
    
    /// <inheritdoc/>
    public async Task<Result<Guid>> CreateMemberAsync(MemberCreationDto memberCreationData) {
        var member = memberCreationData.ToMember();

        var result = await _memberRepository.AddAsync(member);
        if (!result.IsSuccess)
            return result;
        var saveResult = await _memberRepository.SaveChangesAsync();
        if (!saveResult.IsSuccess)
            return Result<Guid>.Failure(saveResult.Error);
        
        return result;
    }
}