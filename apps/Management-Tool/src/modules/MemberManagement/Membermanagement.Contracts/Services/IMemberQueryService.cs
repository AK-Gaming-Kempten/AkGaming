using Membermanagement.Contracts.DTO;
using Membermanagement.Contracts.Enums;

namespace Membermanagement.Contracts.Services;

public interface IMemberQueryService {
    
    Task<MemberDto> GetMemberByGuidAsync(Guid id);
    
    Task<ICollection<MemberDto>> GetAllMembersAsync();
    
    Task<ICollection<MemberDto>> GetMembersByStatusAsync(MembershipStatus status);
    
    Task<ICollection<MemberDto>> GetMembersByStatusAsync(ICollection<MembershipStatus> statuses);
}