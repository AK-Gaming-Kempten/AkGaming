using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

namespace AkGaming.Management.Modules.MemberManagement.Contracts.Services;

public interface IMemberLinkingService {
    
    /// <summary>
    /// Links a <see cref="Member"/> to a <see cref="User"/>
    /// </summary>
    /// <param name="memberId">The id of the <see cref="Member"/></param>
    /// <param name="userId">The id of the <see cref="User"/></param>
    Task<Result> LinkMemberToUserAsync(Guid memberId, Guid userId);
    
    /// <summary>
    /// Unlinks a <see cref="Member"/> from a <see cref="User"/>
    /// </summary>
    /// <param name="memberId">The id of the <see cref="Member"/></param>
    /// <param name="userId">The id of the <see cref="User"/></param>
    Task<Result> UnlinkMemberFromUserAsync(Guid memberId, Guid userId);

    /// <summary>
    /// Creates a <see cref="MemberLinkingRequest"/> from a <see cref="MemberLinkingRequestDto"/>/>
    /// </summary>
    /// <param name="request">The <see cref="MemberLinkingRequestDto"/></param>
    Task<Result> CreateMemberLinkingRequestAsync(MemberLinkingRequestDto request, Guid? performedByUserId = null);
    
    /// <summary>
    /// Gets all <see cref="MemberLinkingRequest"/>
    /// </summary>
    Task<Result<ICollection<MemberLinkingRequestDto>>> GetAllMemberLinkingRequestsAsync();
    
    /// <summary>
    /// Gets all <see cref="MemberLinkingRequest"/> from a <see cref="User"/>
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<Result<ICollection<MemberLinkingRequestDto>>> GetMemberLinkingRequestsFromUserAsync(Guid userId);
    
    /// <summary>
    /// Marks a <see cref="MemberLinkingRequest"/> as resolved
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<Result> MarkMemberLinkingRequestResolvedAsync(Guid id, Guid? performedByUserId = null);
}
