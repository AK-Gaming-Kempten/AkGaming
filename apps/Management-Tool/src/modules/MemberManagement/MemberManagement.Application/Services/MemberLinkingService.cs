using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using Membermanagement.Contracts.Services;
using MemberManagement.Domain.Entities;
using UserManagement.Contracts.Services;
using UserManagement.Domain.Entities;

namespace MemberManagement.Application.Services;

/// <summary>
/// Service for linking a <see cref="Member"/> to a <see cref="User"/>
/// </summary>
public class MemberLinkingService : IMemberLinkingService {
    private readonly IMemberRepository _members;
    private readonly IUserService _users;

    public MemberLinkingService(IMemberRepository members, IUserService users) {
        _members = members;
        _users = users;
    }

    /// <inheritdoc/>
    public async Task<Result> LinkMemberToUserAsync(Guid memberId, Guid userId) {
        var memberResult = await _members.GetByMemberIdAsync(memberId);
        if (!memberResult.IsSuccess)
            return memberResult;
        var member = memberResult.Value!;

        var userResult = await _users.GetUserByIdAsync(userId);
        if (!userResult.IsSuccess)
            return userResult;
        var user = userResult.Value!;

        member.UserId = user.Id;
        await _members.UpdateAsync(member);
        await _members.SaveChangesAsync();
        return Result.Success();
    }
}
