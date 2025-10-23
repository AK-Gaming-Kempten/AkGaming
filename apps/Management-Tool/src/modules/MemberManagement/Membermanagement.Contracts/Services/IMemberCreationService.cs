using Membermanagement.Contracts.DTO;

namespace MemberManagement.Contracts.Services;

public interface IMemberCreationService {
    Task CreateMemberFromUserAsync(Guid userId);
    Task CreateMemberAsync(MemberCreationDto memberCreationData);
}