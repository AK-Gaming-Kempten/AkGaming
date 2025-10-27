using AKG.Common.Extensions;
using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using MemberManagement.Contracts.Services;
using MemberManagement.Domain.Entities;
using UserManagement.Contracts.Services;
using UserManagement.Domain.Entities;

namespace MemberManagement.Application.Services;

/// <summary>
/// Service for linking a <see cref="Member"/> to a <see cref="User"/>
/// </summary>
public class MemberLinkingService : IMemberLinkingService {
    private readonly IMemberRepository _members;

    public MemberLinkingService(IMemberRepository members) {
        _members = members;
    }

    /// <inheritdoc/>
    public async Task<Result> LinkMemberToUserAsync(Guid memberId, Guid userId) {
        var memberResult = await _members.GetByMemberIdAsync(memberId);
        if (!memberResult.IsSuccess)
            return memberResult;
        var member = memberResult.Value!;
        
        member.UserId = userId;
        var result = await _members.UpdateAsync(member)
            .Then( () => _members.SaveChangesAsync());
        
        return result;
    }
}
