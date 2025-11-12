using AKG.Common.Generics;
using MemberManagement.Contracts.DTO;

namespace MemberManagement.Contracts.Services;

public interface IMembershipApplicationService {
    Task<Result> ApplyForMembershipAsync(MembershipApplicationRequestDto request);
    
    Task<Result<ICollection<MembershipApplicationRequestDto>>> GetAllRequestFromUserAsync(Guid userId);
    
    Task<Result<ICollection<MembershipApplicationRequestDto>>> GetAllRequestAsync();
    
    Task<Result> AcceptMembershipApplicationAsync(Guid id);
}