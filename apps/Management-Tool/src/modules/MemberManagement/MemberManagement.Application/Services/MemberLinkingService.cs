using AKG.Common.Extensions;
using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using MemberManagement.Application.Mapping;
using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Services;
using MemberManagement.Domain.Entities;
using System.Text.Json;

namespace MemberManagement.Application.Services;

/// <summary>
/// Service for linking a <see cref="Member"/> to a user
/// </summary>
public class MemberLinkingService : IMemberLinkingService {
    private readonly IMemberRepository _memberRepository;
    private readonly IMemberLinkingRequestRepository _linkingRequestRepository;
    private readonly IMemberAuditLogWriter _auditLogWriter;

    public MemberLinkingService(
        IMemberRepository memberRepository,
        IMemberLinkingRequestRepository linkingRequestRepository,
        IMemberAuditLogWriter auditLogWriter)
    {
        _memberRepository = memberRepository;
        _linkingRequestRepository = linkingRequestRepository;
        _auditLogWriter = auditLogWriter;
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
        var result = await _linkingRequestRepository.GetByIdAsync(id);
        if (!result.IsSuccess)
            return result;
        var request = result.Value!;
        
        if (request.IsResolved)
            return Result.Success();
        
        request.IsResolved = true;
        return await _auditLogWriter.Add(new MemberAuditLog {
            ActionType = "MemberLinkingRequestAccepted",
            PerformedByUserId = performedByUserId,
            EntityType = nameof(MemberLinkingRequest),
            EntityId = request.Id,
            OldValuesJson = JsonSerializer.Serialize(new { IsResolved = false }),
            NewValuesJson = JsonSerializer.Serialize(new { IsResolved = true })
        }).Then(() => _linkingRequestRepository.SaveChangesAsync());
    }
}
