using MemberManagement.Application.Interfaces;
using Membermanagement.Contracts.DTO;
using MemberManagement.Contracts.Services;

namespace MemberManagement.Application.Services;

public class MemberCreationService : IMemberCreationService {
    private readonly IMemberRepository _members;
    
    public MemberCreationService(IMemberRepository members) {
        _members = members;
    }
    
    /// <inheritdoc/>
    public async Task CreateMemberAsync(MemberCreationDto memberCreationData) {
        throw new NotImplementedException();
    }
    
    /// <inheritdoc/>
    public async Task CreateMemberFromUserAsync(Guid userId) {
        throw new NotImplementedException();
    }
}