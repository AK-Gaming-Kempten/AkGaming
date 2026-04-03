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

namespace AkGaming.Management.Modules.MemberManagement.Application.Services;

/// <summary>
/// Service for linking a <see cref="Member"/> to a user
/// </summary>
public class MemberLinkingService : IMemberLinkingService {
    private static readonly string BoardEmail = ClubConstants.EmailAddresses.Board;
    private readonly IMemberRepository _memberRepository;
    private readonly IMemberLinkingRequestRepository _linkingRequestRepository;
    private readonly IMemberAuditLogWriter _auditLogWriter;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<MemberLinkingService> _logger;

    public MemberLinkingService(
        IMemberRepository memberRepository,
        IMemberLinkingRequestRepository linkingRequestRepository,
        IMemberAuditLogWriter auditLogWriter,
        IEmailSender emailSender,
        ILogger<MemberLinkingService> logger)
    {
        _memberRepository = memberRepository;
        _linkingRequestRepository = linkingRequestRepository;
        _auditLogWriter = auditLogWriter;
        _emailSender = emailSender;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> LinkMemberToUserAsync(Guid memberId, Guid userId) {
        var memberResult = await _memberRepository.GetByMemberIdAsync(memberId);
        if (!memberResult.IsSuccess)
            return memberResult;
        var member = memberResult.Value!;
        
        member.UserId = userId;
        var result = await _memberRepository.SaveChangesAsync();
        
        return result;
    }
    
    /// <inheritdoc/>
    public async Task<Result> UnlinkMemberFromUserAsync(Guid memberId, Guid userId) {
        var memberResult = await _memberRepository.GetByMemberIdAsync(memberId);
        if (!memberResult.IsSuccess)
            return memberResult;
        var member = memberResult.Value!;
        
        member.UserId = null;
        var result = await _memberRepository.SaveChangesAsync();
        
        return result;
    }
    
    /// <inheritdoc/>
    public async Task<Result> CreateMemberLinkingRequestAsync(MemberLinkingRequestDto request, Guid? performedByUserId = null) {
        if (!request.PrivacyPolicyAccepted)
            return Result.Failure("Privacy policy must be accepted.");

        var linkingRequest = request.ToMemberLinkingRequest();
        var result = await _linkingRequestRepository.Add(linkingRequest)
            .Then(() => _auditLogWriter.Add(new MemberAuditLog {
                ActionType = "MemberLinkingRequestCreated",
                PerformedByUserId = performedByUserId,
                EntityType = nameof(MemberLinkingRequest),
                EntityId = linkingRequest.Id,
                NewValuesJson = JsonSerializer.Serialize(new {
                    linkingRequest.IssuingUserId,
                    linkingRequest.FirstName,
                    linkingRequest.LastName,
                    linkingRequest.Email,
                    linkingRequest.DiscordUserName,
                    linkingRequest.Reason
                })
            }))
            .Then(() => _linkingRequestRepository.SaveChangesAsync());

        if (!result.IsSuccess)
            return result;

        await SendMemberLinkingRequestCreatedNotificationEmailAsync(linkingRequest);
        return Result.Success();
    }
    
    /// <inheritdoc/>
    public async Task<Result<ICollection<MemberLinkingRequestDto>>> GetAllMemberLinkingRequestsAsync() {
        var result = await _linkingRequestRepository.GetAllAsync();
        if (!result.IsSuccess)
            return Result<ICollection<MemberLinkingRequestDto>>.Failure(result.Error ?? "No Member linking requests found");
        return Result<ICollection<MemberLinkingRequestDto>>.Success(result.Value!.Select(x => x.ToDto()).ToList());
    }
    
    /// <inheritdoc/>
    public async Task<Result<ICollection<MemberLinkingRequestDto>>> GetMemberLinkingRequestsFromUserAsync(Guid userId) {
        var result = await _linkingRequestRepository.GetAllRequestFromUserAsync(userId);
        if (!result.IsSuccess)
            return Result<ICollection<MemberLinkingRequestDto>>.Failure(result.Error ?? "Member linking requests not found for user");
        return Result<ICollection<MemberLinkingRequestDto>>.Success(result.Value!.Select(x => x.ToDto()).ToList());
    }
    
    /// <inheritdoc/>
    public async Task<Result> MarkMemberLinkingRequestResolvedAsync(Guid id, Guid? performedByUserId = null) {
        return await AcceptMemberLinkingRequestAsync(id, performedByUserId);
    }

    public async Task<Result> AcceptMemberLinkingRequestAsync(Guid id, Guid? performedByUserId = null) {
        var result = await _linkingRequestRepository.GetByIdAsync(id);
        if (!result.IsSuccess)
            return result;
        var request = result.Value!;
        
        if (request.IsResolved)
            return Result.Success();
        
        request.IsResolved = true;
        var saveResult = await _auditLogWriter.Add(new MemberAuditLog {
            ActionType = "MemberLinkingRequestAccepted",
            PerformedByUserId = performedByUserId,
            EntityType = nameof(MemberLinkingRequest),
            EntityId = request.Id,
            OldValuesJson = JsonSerializer.Serialize(new { IsResolved = false }),
            NewValuesJson = JsonSerializer.Serialize(new { IsResolved = true })
        }).Then(() => _linkingRequestRepository.SaveChangesAsync());

        if (!saveResult.IsSuccess)
            return saveResult;

        await SendMemberLinkingDecisionEmailAsync(request.Email, accepted: true);
        return Result.Success();
    }

    public async Task<Result> RejectMemberLinkingRequestAsync(Guid id, Guid? performedByUserId = null) {
        var result = await _linkingRequestRepository.GetByIdAsync(id);
        if (!result.IsSuccess)
            return result;
        var request = result.Value!;

        if (request.IsResolved)
            return Result.Success();

        request.IsResolved = true;
        var saveResult = await _auditLogWriter.Add(new MemberAuditLog {
            ActionType = "MemberLinkingRequestRejected",
            PerformedByUserId = performedByUserId,
            EntityType = nameof(MemberLinkingRequest),
            EntityId = request.Id,
            OldValuesJson = JsonSerializer.Serialize(new { IsResolved = false }),
            NewValuesJson = JsonSerializer.Serialize(new { IsResolved = true })
        }).Then(() => _linkingRequestRepository.SaveChangesAsync());

        if (!saveResult.IsSuccess)
            return saveResult;

        await SendMemberLinkingDecisionEmailAsync(request.Email, accepted: false);
        return Result.Success();
    }

    private async Task SendMemberLinkingDecisionEmailAsync(string? recipientEmail, bool accepted) {
        if (string.IsNullOrWhiteSpace(recipientEmail))
            return;

        var decisionText = accepted ? "accepted" : "declined";
        var subject = accepted
            ? $"{ClubConstants.Organization.LegalName} member linking request accepted"
            : $"{ClubConstants.Organization.LegalName} member linking request declined";
        var updatePersonalDataTextBody = accepted
            ? $"Please update your personal data at {ClubConstants.Urls.ManagementMembership}.\n\n"
            : string.Empty;
        var updatePersonalDataHtmlBody = accepted
            ? $"<p style=\"margin:0 0 12px\">Please <a href=\"{ClubConstants.Urls.ManagementMembership}\">update your personal data</a>.</p>"
            : string.Empty;
        var textBody =
            "Hello,\n\n" +
            $"your {ClubConstants.Organization.LegalName} member linking request has been {decisionText}.\n\n" +
            updatePersonalDataTextBody +
            $"If you have questions, please contact us at {BoardEmail}.\n\n" +
            $"Kind regards,\n{ClubConstants.Organization.LegalName}";
        var htmlBody =
            "<div style=\"font-family:Arial,Helvetica,sans-serif;color:#222;line-height:1.6\">" +
            "<p style=\"margin:0 0 12px\">Hello,</p>" +
            $"<p style=\"margin:0 0 12px\">Your {ClubConstants.Organization.LegalName} member linking request has been <strong>{decisionText}</strong>.</p>" +
            updatePersonalDataHtmlBody +
            $"<p style=\"margin:0 0 12px\">If you have questions, please contact us at {BoardEmail}.</p>" +
            $"<p style=\"margin:0\">Kind regards,<br/>{ClubConstants.Organization.LegalName}</p>" +
            "</div>";

        try {
            await _emailSender.SendAsync(recipientEmail, subject, textBody, htmlBody, CancellationToken.None);
        }
        catch (Exception exception) {
            _logger.LogError(exception, "Failed to send member linking decision email to {Email}.", recipientEmail);
        }
    }

    private async Task SendMemberLinkingRequestCreatedNotificationEmailAsync(MemberLinkingRequest request) {
        var subject = $"{ClubConstants.Organization.LegalName} new member linking request";
        var textBody =
            "A new member linking request was created.\n\n" +
            $"Open requests in admin panel: {ClubConstants.Urls.ManagementMemberRequests}\n\n" +
            $"RequestId: {request.Id}\n" +
            $"UserId: {request.IssuingUserId}\n" +
            $"Name: {request.FirstName} {request.LastName}\n" +
            $"Email: {request.Email}\n" +
            $"Discord: {request.DiscordUserName}\n" +
            $"Reason: {request.Reason}\n";
        var htmlBody =
            "<div style=\"font-family:Arial,Helvetica,sans-serif;color:#222;line-height:1.6\">" +
            "<p style=\"margin:0 0 12px\">A new member linking request was created.</p>" +
            $"<p style=\"margin:0 0 12px\"><a href=\"{ClubConstants.Urls.ManagementMemberRequests}\">Open requests in admin panel</a></p>" +
            "<table style=\"border-collapse:collapse\">" +
            $"<tr><td style=\"padding:2px 8px 2px 0\"><strong>RequestId</strong></td><td style=\"padding:2px 0\">{request.Id}</td></tr>" +
            $"<tr><td style=\"padding:2px 8px 2px 0\"><strong>UserId</strong></td><td style=\"padding:2px 0\">{request.IssuingUserId}</td></tr>" +
            $"<tr><td style=\"padding:2px 8px 2px 0\"><strong>Name</strong></td><td style=\"padding:2px 0\">{request.FirstName} {request.LastName}</td></tr>" +
            $"<tr><td style=\"padding:2px 8px 2px 0\"><strong>Email</strong></td><td style=\"padding:2px 0\">{request.Email}</td></tr>" +
            $"<tr><td style=\"padding:2px 8px 2px 0\"><strong>Discord</strong></td><td style=\"padding:2px 0\">{request.DiscordUserName}</td></tr>" +
            $"<tr><td style=\"padding:2px 8px 2px 0\"><strong>Reason</strong></td><td style=\"padding:2px 0\">{request.Reason}</td></tr>" +
            "</table>" +
            "</div>";

        try {
            await _emailSender.SendAsync(BoardEmail, subject, textBody, htmlBody, CancellationToken.None);
        }
        catch (Exception exception) {
            _logger.LogError(exception, "Failed to send member linking request created notification to {Email}.", BoardEmail);
        }
    }
}
