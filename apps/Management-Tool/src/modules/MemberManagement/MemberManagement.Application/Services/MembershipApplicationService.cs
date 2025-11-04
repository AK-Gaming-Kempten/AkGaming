using AKG.Common.Generics;
using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Services;
using ContractEnums = MemberManagement.Contracts.Enums ; 
using DomainEnums = MemberManagement.Domain.Enums;

namespace MemberManagement.Application.Services;

public class MembershipApplicationService : IMembershipApplicationService {
    private readonly IMemberCreationService _creationService;
    private readonly IMemberLinkingService _linkingService;
    private readonly IMembershipUpdateService _membershipUpdateService;

    public MembershipApplicationService(
        IMemberCreationService creationService,
        IMemberLinkingService linkingService,
        IMembershipUpdateService membershipUpdateService)
    {
        _creationService  = creationService;
        _linkingService    = linkingService;
        _membershipUpdateService  = membershipUpdateService;
    }

    public async Task<Result<Guid>> ApplyForMembershipAsync(Guid userId, MemberCreationDto dto) {
        // Create Member
        var memberCreationResult = await _creationService.CreateMemberAsync(dto);
        if (!memberCreationResult.IsSuccess)
            return Result<Guid>.Failure(memberCreationResult.Error);

        var memberId = memberCreationResult.Value;

        // Link User
        var linkResult = await _linkingService.LinkMemberToUserAsync(memberId, userId);
        if (!linkResult.IsSuccess)
            return Result<Guid>.Failure(linkResult.Error);

        // Update Status
        var statusResult = await _membershipUpdateService.UpdateMembershipStatusAsync(
            memberId,
            ContractEnums.MembershipStatus.Applicant
        );
        if (!statusResult.IsSuccess)
            return Result<Guid>.Failure(statusResult.Error);

        return Result<Guid>.Success(memberId);
    }
}