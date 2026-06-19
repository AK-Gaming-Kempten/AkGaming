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

    public async Task<Result<Guid>> CreateUserProfileAsync(Guid userId) {
        var existingMemberResult = await _memberRepository.GetByUserIdAsync(userId);
        if (existingMemberResult.IsSuccess) {
            return Result<Guid>.Success(existingMemberResult.Value!.Id);
        }
        if (!string.Equals(existingMemberResult.Error, "Member not found.", StringComparison.Ordinal)) {
            return Result<Guid>.Failure(existingMemberResult.Error ?? "Profile lookup failed");
        }

        var profile = new Member {
            UserId = userId,
            Address = new Address()
        };

        var addResult = _memberRepository.Add(profile);
        if (!addResult.IsSuccess) {
            return Result<Guid>.Failure(addResult.Error ?? "Profile could not be created");
        }

        var saveResult = await _memberRepository.SaveChangesAsync();
        return saveResult.IsSuccess
            ? Result<Guid>.Success(profile.Id)
            : Result<Guid>.Failure(saveResult.Error ?? "Profile could not be saved");
    }
}
