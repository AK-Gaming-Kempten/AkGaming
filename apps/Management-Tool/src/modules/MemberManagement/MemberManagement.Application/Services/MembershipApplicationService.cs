using AKG.Common.Extensions;
using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using MemberManagement.Application.Mapping;
using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Services;
using MemberManagement.Domain.Entities;
using ContractEnums = MemberManagement.Contracts.Enums ; 
using DomainEnums = MemberManagement.Domain.Enums;

namespace MemberManagement.Application.Services;

public class MembershipApplicationService : IMembershipApplicationService {
    private readonly IMemberCreationService _creationService;
    private readonly IMemberLinkingService _linkingService;
    private readonly IMembershipUpdateService _membershipUpdateService;
    private readonly IMemberQueryService _memberQueryService;
    private readonly IMembershipApplicationRequestRepository _membershipApplicationRequestRepository;

    public MembershipApplicationService(
        IMemberCreationService creationService,
        IMemberLinkingService linkingService,
        IMembershipUpdateService membershipUpdateService,
        IMemberQueryService memberQueryService,
        IMembershipApplicationRequestRepository membershipApplicationRequestRepository)
    {
        _creationService  = creationService;
        _linkingService    = linkingService;
        _membershipUpdateService  = membershipUpdateService;
        _memberQueryService = memberQueryService;
        _membershipApplicationRequestRepository = membershipApplicationRequestRepository;
    }

    public async Task<Result> ApplyForMembershipAsync(MembershipApplicationRequestDto request) {
        // Check if user is already a member or has a pending application / linking request
        var memberResult = await _memberQueryService.GetMemberByUserGuidAsync(request.IssuingUserId);
        if (memberResult.IsSuccess)
            return Result.Failure("User is already a member");
        
        var pendingRequestResult = await _membershipApplicationRequestRepository.GetAllRequestFromUserAsync(request.IssuingUserId);
        if (pendingRequestResult.IsSuccess && pendingRequestResult.Value!.Where(x => !x.IsResolved).ToList().Count > 0)
            return Result.Failure("User has a pending application");
        
        var linkingRequestResult = await _linkingService.GetMemberLinkingRequestsFromUserAsync(request.IssuingUserId);
        if (linkingRequestResult.IsSuccess && linkingRequestResult.Value!.Where(x => !x.IsResolved).ToList().Count > 0)
            return Result.Failure("User has a pending linking request");
        
        // Create Request
        var requestResult = await CreateMembershipApplicationRequestAsync(request);
        if (!requestResult.IsSuccess)
            return Result.Failure(requestResult.Error ?? "Membership application request could not be created");
        
        // Create Member
        var memberCreationResult = await _creationService.CreateMemberAsync(request.MemberCreationInfo);
        if (!memberCreationResult.IsSuccess)
            return Result.Failure(memberCreationResult.Error ?? "Member could not be created");

        var memberId = memberCreationResult.Value;

        // Link User
        var linkResult = await _linkingService.LinkMemberToUserAsync(memberId, request.IssuingUserId);
        if (!linkResult.IsSuccess)
            return Result.Failure(linkResult.Error ?? "Member could not be linked to user");

        // Update Status
        var statusResult = await _membershipUpdateService.UpdateMembershipStatusAsync(
            memberId,
            ContractEnums.MembershipStatus.Applicant
        );
        if (!statusResult.IsSuccess)
            return Result.Failure(statusResult.Error ?? "Membership status could not be updated");

        return Result.Success();
    }
    
    public async Task<Result<ICollection<MembershipApplicationRequestDto>>> GetAllRequestAsync() {
        var result = await _membershipApplicationRequestRepository.GetAllAsync();
        if (!result.IsSuccess)
            return Result<ICollection<MembershipApplicationRequestDto>>.Failure(result.Error ?? "Membership application requests not found");
        var requests = result.Value!;
        
        return Result<ICollection<MembershipApplicationRequestDto>>.Success(requests.Select(m => m.ToDto()).ToList());
    }

    public async Task<Result<ICollection<MembershipApplicationRequestDto>>> GetAllRequestFromUserAsync(Guid userId) {
        var result = await _membershipApplicationRequestRepository.GetAllRequestFromUserAsync(userId);
        if (!result.IsSuccess)
            return Result<ICollection<MembershipApplicationRequestDto>>.Failure(result.Error ?? "Membership application requests not found");
        var requests = result.Value!;
        
        return Result<ICollection<MembershipApplicationRequestDto>>.Success(requests.Select(m => m.ToDto()).ToList());
    }

    public async Task<Result> AcceptMembershipApplicationAsync(Guid id) {
        // Get Request by Id
        var requestResult = await _membershipApplicationRequestRepository.GetByIdAsync(id);
        if (!requestResult.IsSuccess)
            return requestResult;
        var request = requestResult.Value!;

        // Get Member from request
        var memberResult = await _memberQueryService.GetMemberByUserGuidAsync(request.IssuingUserId);
        if (!memberResult.IsSuccess)
            return memberResult;
        var member = memberResult.Value!;
        
        // Update Status
        var statusResult = await _membershipUpdateService.UpdateMembershipStatusAsync(member.Id, ContractEnums.MembershipStatus.InTrial);
        if (!statusResult.IsSuccess)
            return statusResult;
        
        // Set Request as accepted
        request.IsResolved = true;
        return await _membershipApplicationRequestRepository.SaveChangesAsync();
    }
    
    public async Task<Result> RejectMembershipApplicationAsync(Guid id) {
        // Get Request by Id
        var requestResult = await _membershipApplicationRequestRepository.GetByIdAsync(id);
        if (!requestResult.IsSuccess)
            return requestResult;
        var request = requestResult.Value!;
        
        // Get Member from request
        var memberResult = await _memberQueryService.GetMemberByUserGuidAsync(request.IssuingUserId);
        if (!memberResult.IsSuccess)
            return memberResult;
        var member = memberResult.Value!;
        
        // Update Status
        var statusResult = await _membershipUpdateService.UpdateMembershipStatusAsync(member.Id, ContractEnums.MembershipStatus.ApplicationRejected);
        if (!statusResult.IsSuccess)
            return statusResult;
        
        // Set Request as rejected
        request.IsResolved = true;
        return await _membershipApplicationRequestRepository.SaveChangesAsync();
    }
    
    private async Task<Result> CreateMembershipApplicationRequestAsync(MembershipApplicationRequestDto requestDto) {
        var request = requestDto.ToMembershipApplicationRequest();
        var result = await _membershipApplicationRequestRepository.Add(request)
            .Then(() => _membershipApplicationRequestRepository.SaveChangesAsync());
        return result;
    }
}