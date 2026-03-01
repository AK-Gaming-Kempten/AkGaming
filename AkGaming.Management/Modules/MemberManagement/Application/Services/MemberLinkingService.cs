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

namespace AkGaming.Management.Modules.MemberManagement.Application.Services;

/// <summary>
/// Service for linking a <see cref="Member"/> to a user
/// </summary>
public class MemberLinkingService : IMemberLinkingService {
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
        return await _linkingRequestRepository.Add(linkingRequest)
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
            ? "AK Gaming e.V. member linking request accepted"
            : "AK Gaming e.V. member linking request declined";
        var textBody =
            "Hello,\n\n" +
            $"your AK Gaming e.V. member linking request has been {decisionText}.\n\n" +
            "If you have questions, please contact us at vorstand@akgaming.de .\n\n" +
            "Kind regards,\nAK Gaming e.V.";
        var htmlBody =
            "<div style=\"font-family:Arial,Helvetica,sans-serif;color:#222;line-height:1.6\">" +
            "<p style=\"margin:0 0 12px\">Hello,</p>" +
            $"<p style=\"margin:0 0 12px\">Your AK Gaming e.V. member linking request has been <strong>{decisionText}</strong>.</p>" +
            "<p style=\"margin:0 0 12px\">If you have questions, please contact us at vorstand@akgaming.de.</p>" +
            "<p style=\"margin:0\">Kind regards,<br/>AK Gaming  e.V.</p>" +
            "</div>";

        try {
            await _emailSender.SendAsync(recipientEmail, subject, textBody, htmlBody, CancellationToken.None);
        }
        catch (Exception exception) {
            _logger.LogError(exception, "Failed to send member linking decision email to {Email}.", recipientEmail);
        }
    }
}
