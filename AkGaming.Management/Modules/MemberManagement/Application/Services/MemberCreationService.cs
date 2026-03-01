using AkGaming.Core.Common.Extensions;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Application.Mapping;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using AkGaming.Management.Modules.MemberManagement.Domain.ValueObjects;

namespace AkGaming.Management.Modules.MemberManagement.Application.Services;

public class MemberCreationService : IMemberCreationService {
    private readonly IMemberRepository _memberRepository;
    
    public MemberCreationService(IMemberRepository memberRepository) {
        _memberRepository = memberRepository;
    }
    
    /// <inheritdoc/>
    public async Task<Result<Guid>> CreateMemberAsync(MemberCreationDto memberCreationData) {
        var member = memberCreationData.ToMember();

        var result = _memberRepository.Add(member);
        if (!result.IsSuccess)
            return Result<Guid>.Failure(result.Error ?? "Member could not be created");
        var saveResult = await _memberRepository.SaveChangesAsync();
        if (!saveResult.IsSuccess)
            return Result<Guid>.Failure(saveResult.Error ?? "Member could not be saved");
        
        return Result<Guid>.Success(member.Id);
    }
}