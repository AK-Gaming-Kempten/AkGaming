using AkGaming.Core.Common.Extensions;
using AkGaming.Core.Common.Email;
using AkGaming.Core.Common.Generics;
using AkGaming.Core.Constants;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Application.Mapping;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ContractEnums = AkGaming.Management.Modules.MemberManagement.Contracts.Enums ; 

namespace AkGaming.Management.Modules.MemberManagement.Application.Services;

public class MembershipApplicationService : IMembershipApplicationService {
    private static readonly string BoardEmail = ClubConstants.EmailAddresses.Board;
    private readonly IMemberCreationService _creationService;
    private readonly IMemberLinkingService _linkingService;
    private readonly IMembershipUpdateService _membershipUpdateService;
    private readonly IMemberQueryService _memberQueryService;
    private readonly IMembershipApplicationRequestRepository _membershipApplicationRequestRepository;
    private readonly IMemberAuditLogWriter _auditLogWriter;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<MembershipApplicationService> _logger;
    private readonly IMemberNotificationOutbox _notificationOutbox;

    public MembershipApplicationService(
        IMemberCreationService creationService,
        IMemberLinkingService linkingService,
        IMembershipUpdateService membershipUpdateService,
        IMemberQueryService memberQueryService,
        IMembershipApplicationRequestRepository membershipApplicationRequestRepository,
        IMemberAuditLogWriter auditLogWriter,
        IEmailSender emailSender,
        ILogger<MembershipApplicationService> logger,
        IMemberNotificationOutbox? notificationOutbox = null)
    {
        _creationService  = creationService;
        _linkingService    = linkingService;
        _membershipUpdateService  = membershipUpdateService;
        _memberQueryService = memberQueryService;
        _membershipApplicationRequestRepository = membershipApplicationRequestRepository;
        _auditLogWriter = auditLogWriter;
        _emailSender = emailSender;
        _logger = logger;
        _notificationOutbox = notificationOutbox ?? new NullMemberNotificationOutbox();
    }

    public async Task<Result> ApplyForMembershipAsync(MembershipApplicationRequestDto request, Guid? performedByUserId = null) {
        if (!request.PrivacyPolicyAccepted)
            return Result.Failure("Privacy policy must be accepted.");

        // Status-None profiles and rejected applications can be promoted back to applicant.
        var memberResult = await _memberQueryService.GetMemberByUserGuidAsync(request.IssuingUserId);
        if (memberResult.IsSuccess && !CanApplyForMembership(memberResult.Value!.Status))
            return Result.Failure("User already has a membership record");
        
        var pendingRequestResult = await _membershipApplicationRequestRepository.GetAllRequestFromUserAsync(request.IssuingUserId);
        if (pendingRequestResult.IsSuccess && pendingRequestResult.Value!.Where(x => !x.IsResolved).ToList().Count > 0)
            return Result.Failure("User has a pending application");
        
        var linkingRequestResult = await _linkingService.GetMemberLinkingRequestsFromUserAsync(request.IssuingUserId);
        if (linkingRequestResult.IsSuccess && linkingRequestResult.Value!.Where(x => !x.IsResolved).ToList().Count > 0)
            return Result.Failure("User has a pending linking request");
        
        // Create Request
        var requestResult = await CreateMembershipApplicationRequestAsync(request, performedByUserId);
        if (!requestResult.IsSuccess)
            return Result.Failure(requestResult.Error ?? "Membership application request could not be created");
        
        Guid memberId;
        if (memberResult.IsSuccess) {
            memberId = memberResult.Value!.Id;
        }
        else {
            var memberCreationResult = await _creationService.CreateMemberAsync(request.MemberCreationInfo);
            if (!memberCreationResult.IsSuccess)
                return Result.Failure(memberCreationResult.Error ?? "Member could not be created");

            memberId = memberCreationResult.Value;
            var linkResult = await _linkingService.LinkMemberToUserAsync(memberId, request.IssuingUserId);
            if (!linkResult.IsSuccess)
                return Result.Failure(linkResult.Error ?? "Member could not be linked to user");
        }

        // Update Status
        var statusResult = await _membershipUpdateService.UpdateMembershipStatusAsync(
            memberId,
            ContractEnums.MembershipStatus.Applicant
        );
        if (!statusResult.IsSuccess)
            return Result.Failure(statusResult.Error ?? "Membership status could not be updated");

        return Result.Success();
    }

    private static bool CanApplyForMembership(ContractEnums.MembershipStatus status) =>
        status is ContractEnums.MembershipStatus.None or ContractEnums.MembershipStatus.ApplicationRejected;
    
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

    public async Task<Result> AcceptMembershipApplicationAsync(Guid id, Guid? performedByUserId = null) {
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
        var result = await _auditLogWriter.Add(new MemberAuditLog {
            ActionType = "MembershipApplicationRequestAccepted",
            PerformedByUserId = performedByUserId,
            EntityType = nameof(MembershipApplicationRequest),
            EntityId = request.Id,
            OldValuesJson = JsonSerializer.Serialize(new { IsResolved = false }),
            NewValuesJson = JsonSerializer.Serialize(new { IsResolved = true })
        }).Then(() => _membershipApplicationRequestRepository.SaveChangesAsync());

        if (!result.IsSuccess)
            return result;

        await SendMembershipApplicationDecisionEmailAsync(request.Email, accepted: true);
        return Result.Success();
    }
    
    public async Task<Result> RejectMembershipApplicationAsync(Guid id, Guid? performedByUserId = null) {
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
        var result = await _auditLogWriter.Add(new MemberAuditLog {
            ActionType = "MembershipApplicationRequestRejected",
            PerformedByUserId = performedByUserId,
            EntityType = nameof(MembershipApplicationRequest),
            EntityId = request.Id,
            OldValuesJson = JsonSerializer.Serialize(new { IsResolved = false }),
            NewValuesJson = JsonSerializer.Serialize(new { IsResolved = true })
        }).Then(() => _membershipApplicationRequestRepository.SaveChangesAsync());

        if (!result.IsSuccess)
            return result;

        await SendMembershipApplicationDecisionEmailAsync(request.Email, accepted: false);
        return Result.Success();
    }
    
    private async Task<Result> CreateMembershipApplicationRequestAsync(MembershipApplicationRequestDto requestDto, Guid? performedByUserId) {
        var request = requestDto.ToMembershipApplicationRequest();
        var result = await _membershipApplicationRequestRepository.Add(request)
            .Then(() => {
                _notificationOutbox.EnqueueMembershipApplicationCreated(request);
                return Result.Success();
            })
            .Then(() => _auditLogWriter.Add(new MemberAuditLog {
                ActionType = "MembershipApplicationRequestCreated",
                PerformedByUserId = performedByUserId,
                EntityType = nameof(MembershipApplicationRequest),
                EntityId = request.Id,
                NewValuesJson = JsonSerializer.Serialize(new {
                    request.IssuingUserId,
                    request.FirstName,
                    request.LastName,
                    request.Email,
                    request.Phone,
                    request.DiscordUserName,
                    request.BirthDate,
                    request.ApplicationText
                })
            }))
            .Then(() => _membershipApplicationRequestRepository.SaveChangesAsync());

        if (!result.IsSuccess)
            return result;

        await SendMembershipApplicationCreatedNotificationEmailAsync(request);
        return Result.Success();
    }

    private async Task SendMembershipApplicationDecisionEmailAsync(string? recipientEmail, bool accepted) {
        if (string.IsNullOrWhiteSpace(recipientEmail))
            return;

        var email = MembershipApplicationEmailComposer.ComposeDecisionEmail(accepted);

        try {
            await _emailSender.SendAsync(recipientEmail, email.Subject, email.TextBody, email.HtmlBody, CancellationToken.None);
        }
        catch (Exception exception) {
            _logger.LogError(exception, "Failed to send membership application decision email to {Email}.", recipientEmail);
        }
    }

    private async Task SendMembershipApplicationCreatedNotificationEmailAsync(MembershipApplicationRequest request) {
        var email = MembershipApplicationEmailComposer.ComposeCreatedNotificationEmail(request);

        try {
            await _emailSender.SendAsync(BoardEmail, email.Subject, email.TextBody, email.HtmlBody, CancellationToken.None);
        }
        catch (Exception exception) {
            _logger.LogError(exception, "Failed to send membership application created notification to {Email}.", BoardEmail);
        }
    }
}
