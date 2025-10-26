using AKG.Common.Generics;
using Membermanagement.Contracts.DTO;
using Membermanagement.Contracts.Enums;

namespace Membermanagement.Contracts.Services;

public interface IMemberQueryService {
    
    Task<Result<MemberDto>> GetMemberByGuidAsync(Guid id);
    
    Task<Result<ICollection<MemberDto>>> GetAllMembersAsync();
    
    Task<Result<ICollection<MemberDto>>> GetMembersWithStatusAsync(MembershipStatus status);
    
    Task<Result<ICollection<MemberDto>>> GetMembersWithStatusAsync(ICollection<MembershipStatus> statuses);
}