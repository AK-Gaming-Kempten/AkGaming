using Membermanagement.Contracts.DTO;
using Membermanagement.Contracts.Services;

namespace MemberManagement.Application.Services;

public class MemberUpdateService : IMemberUpdateService {
    
    public MemberUpdateService() {
    }
    
    public Task UpdateMemberAsync(Guid memberId, MemberDto memberData) {
        throw new NotImplementedException();
    }
}