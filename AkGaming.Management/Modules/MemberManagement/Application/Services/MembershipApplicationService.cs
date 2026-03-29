using AkGaming.Core.Common.Extensions;
using AkGaming.Core.Common.Email;
using AkGaming.Core.Common.Generics;
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
    private const string VorstandEmail = "vorstand@akgaming.de";
    private readonly IMemberCreationService _creationService;
    private readonly IMemberLinkingService _linkingService;
    private readonly IMembershipUpdateService _membershipUpdateService;
    private readonly IMemberQueryService _memberQueryService;
    private readonly IMembershipApplicationRequestRepository _membershipApplicationRequestRepository;
    private readonly IMemberAuditLogWriter _auditLogWriter;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<MembershipApplicationService> _logger;

    public MembershipApplicationService(
        IMemberCreationService creationService,
        IMemberLinkingService linkingService,
        IMembershipUpdateService membershipUpdateService,
        IMemberQueryService memberQueryService,
        IMembershipApplicationRequestRepository membershipApplicationRequestRepository,
        IMemberAuditLogWriter auditLogWriter,
        IEmailSender emailSender,
        ILogger<MembershipApplicationService> logger)
    {
        _creationService  = creationService;
        _linkingService    = linkingService;
        _membershipUpdateService  = membershipUpdateService;
        _memberQueryService = memberQueryService;
        _membershipApplicationRequestRepository = membershipApplicationRequestRepository;
        _auditLogWriter = auditLogWriter;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result> ApplyForMembershipAsync(MembershipApplicationRequestDto request, Guid? performedByUserId = null) {
        if (!request.PrivacyPolicyAccepted)
            return Result.Failure("Privacy policy must be accepted.");

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
        var requestResult = await CreateMembershipApplicationRequestAsync(request, performedByUserId);
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

        var decisionText = accepted ? "accepted" : "declined";
        var subject = accepted
            ? "AK Gaming e.V. membership application accepted"
            : "AK Gaming e.V. membership application declined";
        var textBody =
            "Hello,\n\n" +
            $"your AK Gaming e.V. membership application has been {decisionText}.\n\n" +
            "If you have questions, please contact us at vorstand@akgaming.de.\n\n" +
            "Kind regards,\nAK Gaming e.V.";
        var htmlBody =
            "<div style=\"font-family:Arial,Helvetica,sans-serif;color:#222;line-height:1.6\">" +
            "<p style=\"margin:0 0 12px\">Hello,</p>" +
            $"<p style=\"margin:0 0 12px\">Your AK Gaming e.V. membership application has been <strong>{decisionText}</strong>.</p>" +
            "<p style=\"margin:0 0 12px\">If you have questions, please contact us at vorstand@akgaming.de.</p>" +
            "<p style=\"margin:0\">Kind regards,<br/>AK Gaming e.V.</p>" +
            "</div>";

        try {
            await _emailSender.SendAsync(recipientEmail, subject, textBody, htmlBody, CancellationToken.None);
        }
        catch (Exception exception) {
            _logger.LogError(exception, "Failed to send membership application decision email to {Email}.", recipientEmail);
        }
    }

    private async Task SendMembershipApplicationCreatedNotificationEmailAsync(MembershipApplicationRequest request) {
        const string adminRequestsUrl = "https://management.akgaming.de/member-management/requests";
        var subject = "AK Gaming e.V. new membership application";
        var textBody =
            "A new membership application was created.\n\n" +
            $"Open requests in admin panel: {adminRequestsUrl}\n\n" +
            $"RequestId: {request.Id}\n" +
            $"UserId: {request.IssuingUserId}\n" +
            $"Name: {request.FirstName} {request.LastName}\n" +
            $"Email: {request.Email}\n" +
            $"Phone: {request.Phone}\n" +
            $"Discord: {request.DiscordUserName}\n" +
            $"BirthDate: {request.BirthDate:yyyy-MM-dd}\n" +
            $"ApplicationText: {request.ApplicationText}\n";
        var htmlBody =
            "<div style=\"font-family:Arial,Helvetica,sans-serif;color:#222;line-height:1.6\">" +
            "<p style=\"margin:0 0 12px\">A new membership application was created.</p>" +
            $"<p style=\"margin:0 0 12px\"><a href=\"{adminRequestsUrl}\">Open requests in admin panel</a></p>" +
            "<table style=\"border-collapse:collapse\">" +
            $"<tr><td style=\"padding:2px 8px 2px 0\"><strong>RequestId</strong></td><td style=\"padding:2px 0\">{request.Id}</td></tr>" +
            $"<tr><td style=\"padding:2px 8px 2px 0\"><strong>UserId</strong></td><td style=\"padding:2px 0\">{request.IssuingUserId}</td></tr>" +
            $"<tr><td style=\"padding:2px 8px 2px 0\"><strong>Name</strong></td><td style=\"padding:2px 0\">{request.FirstName} {request.LastName}</td></tr>" +
            $"<tr><td style=\"padding:2px 8px 2px 0\"><strong>Email</strong></td><td style=\"padding:2px 0\">{request.Email}</td></tr>" +
            $"<tr><td style=\"padding:2px 8px 2px 0\"><strong>Phone</strong></td><td style=\"padding:2px 0\">{request.Phone}</td></tr>" +
            $"<tr><td style=\"padding:2px 8px 2px 0\"><strong>Discord</strong></td><td style=\"padding:2px 0\">{request.DiscordUserName}</td></tr>" +
            $"<tr><td style=\"padding:2px 8px 2px 0\"><strong>BirthDate</strong></td><td style=\"padding:2px 0\">{request.BirthDate:yyyy-MM-dd}</td></tr>" +
            $"<tr><td style=\"padding:2px 8px 2px 0\"><strong>ApplicationText</strong></td><td style=\"padding:2px 0\">{request.ApplicationText}</td></tr>" +
            "</table>" +
            "</div>";

        try {
            await _emailSender.SendAsync(VorstandEmail, subject, textBody, htmlBody, CancellationToken.None);
        }
        catch (Exception exception) {
            _logger.LogError(exception, "Failed to send membership application created notification to {Email}.", VorstandEmail);
        }
    }
}
