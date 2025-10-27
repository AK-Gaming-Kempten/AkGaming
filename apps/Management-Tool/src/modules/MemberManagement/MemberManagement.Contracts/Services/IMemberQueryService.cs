using AKG.Common.Generics;
using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Enums;

namespace MemberManagement.Contracts.Services;

public interface IMemberQueryService {
    
    Task<Result<MemberDto>> GetMemberByGuidAsync(Guid id);
    
    Task<Result<ICollection<MemberDto>>> GetAllMembersAsync();
    
    Task<Result<ICollection<MemberDto>>> GetMembersWithStatusAsync(MembershipStatus status);
    
    Task<Result<ICollection<MemberDto>>> GetMembersWithStatusAsync(ICollection<MembershipStatus> statuses);
}