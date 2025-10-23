using MemberManagement.Application.Interfaces;
using Membermanagement.Contracts.Services;
using UserManagement.Contracts.Services;

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
    public async Task LinkMemberToUserAsync(Guid memberId, Guid userId) {
        var member = await _members.GetByIdAsync(memberId);
        if (member is null)
            throw new Exception("Member not found");

        var user = await _users.GetUserByIdAsync(userId);
        if (user is null)
            throw new Exception("User not found");

        member.UserId = user.Id;
        await _members.UpdateAsync(member);
    }
}
