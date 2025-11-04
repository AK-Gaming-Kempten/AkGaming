using AKG.Common.Generics;
using MemberManagement.Contracts.DTO;

namespace MemberManagement.Contracts.Services;

public interface IMembershipApplicationService {
    Task<Result<Guid>> ApplyForMembershipAsync(
        Guid userId,
        MemberCreationDto dto
    );
}