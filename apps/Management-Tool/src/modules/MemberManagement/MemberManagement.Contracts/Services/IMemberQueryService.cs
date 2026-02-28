using AKG.Common.Generics;
using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Enums;

namespace MemberManagement.Contracts.Services;

public interface IMemberQueryService {
    
    /// <summary>
    /// Gets a <see cref="MemberDto"/> by its id
    /// </summary>
    /// <param name="id">The id of the <see cref="MemberDto"/></param>
    /// <returns> The <see cref="MemberDto"/> with the specified id </returns>
    Task<Result<MemberDto>> GetMemberByGuidAsync(Guid id);
    
    /// <summary>
    /// Gets a <see cref="MemberDto"/> by its related user id
    /// </summary>
    /// <param name="id">The user id of the <see cref="MemberDto"/></param>
    /// <returns> The <see cref="MemberDto"/> with the specified user id </returns>
    Task<Result<MemberDto>> GetMemberByUserGuidAsync(Guid id);
    
    /// <summary>
    /// Gets all <see cref="MemberDto"/>
    /// </summary>
    /// <returns>Collection of <see cref="MemberDto"/></returns>
    Task<Result<ICollection<MemberDto>>> GetAllMembersAsync();
    
    /// <summary>
    /// Gets all <see cref="MemberDto"/> with a specific <see cref="MembershipStatus"/>
    /// </summary>
    /// <param name="status"> The <see cref="MembershipStatus"/> the <see cref="MemberDto"/> should have</param>
    /// <returns> Collection of <see cref="MemberDto"/> with the specified <see cref="MembershipStatus"/></returns>
    Task<Result<ICollection<MemberDto>>> GetMembersWithStatusAsync(MembershipStatus status);
    
    /// <summary>
    /// Gets all <see cref="MemberDto"/> with a specific <see cref="MembershipStatus"/>
    /// </summary>
    /// <param name="statuses"> Collection of <see cref="MembershipStatus"/> the <see cref="MemberDto"/> should have</param>
    /// <returns> Collection of <see cref="MemberDto"/> with the specified <see cref="MembershipStatus"/></returns>
    Task<Result<ICollection<MemberDto>>> GetMembersWithStatusAsync(ICollection<MembershipStatus> statuses);
}