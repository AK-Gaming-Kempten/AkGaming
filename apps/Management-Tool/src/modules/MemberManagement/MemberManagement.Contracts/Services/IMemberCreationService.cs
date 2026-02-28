using AKG.Common.Generics;
using MemberManagement.Contracts.DTO;

namespace MemberManagement.Contracts.Services;

public interface IMemberCreationService {
    
    /// <summary>
    /// Creates a new <see cref="Member"/>
    /// </summary>
    /// <param name="memberCreationData">The data for the new <see cref="Member"/></param>
    Task<Result<Guid>> CreateMemberAsync(MemberCreationDto memberCreationData);
}