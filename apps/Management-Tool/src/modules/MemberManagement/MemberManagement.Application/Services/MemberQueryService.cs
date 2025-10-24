using AKG.Common.Generics;
using Membermanagement.Contracts.DTO;
using Membermanagement.Contracts.Services;
using ContractEnums = Membermanagement.Contracts.Enums ; 
using DomainEnums = MemberManagement.Domain.Enums;

namespace MemberManagement.Application.Services;

public class MemberQueryService : IMemberQueryService {
    
    public MemberQueryService() {
    }
    
    public Task<Result<MemberDto>> GetMemberByGuidAsync(Guid id) {
        throw new NotImplementedException();
    }
    
    public Task<Result<ICollection<MemberDto>>> GetAllMembersAsync() {
        throw new NotImplementedException();
    }
    
    public Task<Result<ICollection<MemberDto>>> GetMembersByStatusAsync(ContractEnums.MembershipStatus status) {
        throw new NotImplementedException();
    }
    
    public Task<Result<ICollection<MemberDto>>> GetMembersByStatusAsync(ICollection<ContractEnums.MembershipStatus> statuses) {
        throw new NotImplementedException();
    }
}