using AKG.Common.Generics;

namespace MemberManagement.Contracts.Services;

public interface IMemberLinkingService {
    
    /// <summary>
    /// Links a <see cref="Member"/> to a <see cref="User"/>
    /// </summary>
    /// <param name="memberId">The id of the <see cref="Member"/></param>
    /// <param name="userId">The id of the <see cref="User"/></param>
    Task<Result> LinkMemberToUserAsync(Guid memberId, Guid userId);
}