using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;

namespace AkGaming.Management.Modules.MemberManagement.Contracts.Services;

public interface IMembershipUpdateService {
    
    /// <summary>
    /// Sets a new membership status for a member and logs the change as a new <see cref="MembershipStatusChangeEvent"/>
    /// </summary>
    /// <param name="memberId"> The id of the member</param>
    /// <param name="newStatus"> The status that should be set for the member</param>
    /// <exception cref="NullReferenceException">If the member is not present in the database </exception>
    Task<Result> UpdateMembershipStatusAsync(Guid memberId, MembershipStatus newStatus);
    
    /// <summary>
    /// Returns the default end of trial period for a member
    /// </summary>
    /// <param name="memberId"> The id of the member </param>
    /// <returns> The default end of trial period for the member </returns>
    /// <exception cref="NullReferenceException"> If the member is not present in the database </exception>
    Task<Result<DateTime?>> GetDefaultEndOfTrialPeriodAsync(Guid memberId);
    
    /// <summary>
    /// Inserts a new <see cref="MembershipStatusChangeEvent"/> into the database without updating the member's status
    /// </summary>
    /// <param name="memberId"> The id of the member </param>
    /// <param name="changeEvent"> The event to be inserted </param>
    /// <returns></returns>
    Task<Result> InsertMembershipStatusChangeEventAsync(Guid memberId, MembershipStatusChangeEventDto changeEvent);
    
    /// <summary>
    /// Returns the status changes of a member
    /// </summary>
    /// <param name="memberId"> The id of the member </param>
    /// <returns> The status changes of the member </returns>
    /// <exception cref="NullReferenceException"> If the member is not present in the database </exception>
    Task<Result<List<MembershipStatusChangeEventDto>>> GetMembershipStatusChangesAsync(Guid memberId);
}