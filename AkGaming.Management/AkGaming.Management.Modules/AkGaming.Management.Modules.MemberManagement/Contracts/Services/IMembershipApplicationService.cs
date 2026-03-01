using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

namespace AkGaming.Management.Modules.MemberManagement.Contracts.Services;

public interface IMembershipApplicationService {
    Task<Result> ApplyForMembershipAsync(MembershipApplicationRequestDto request, Guid? performedByUserId = null);
    
    Task<Result<ICollection<MembershipApplicationRequestDto>>> GetAllRequestFromUserAsync(Guid userId);
    
    Task<Result<ICollection<MembershipApplicationRequestDto>>> GetAllRequestAsync();
    
    Task<Result> AcceptMembershipApplicationAsync(Guid id, Guid? performedByUserId = null);
    
    Task<Result> RejectMembershipApplicationAsync(Guid id);
}
