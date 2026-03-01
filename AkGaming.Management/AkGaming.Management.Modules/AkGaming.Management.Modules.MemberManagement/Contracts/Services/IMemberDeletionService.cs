using AkGaming.Core.Common.Generics;

namespace AkGaming.Management.Modules.MemberManagement.Contracts.Services;

public interface IMemberDeletionService {
    
    /// <summary>
    /// Deletes a <see cref="Member"/>
    /// </summary>
    /// <param name="memberId">The id of the <see cref="Member"/></param>
    Task<Result> DeleteMemberAsync(Guid memberId);
}