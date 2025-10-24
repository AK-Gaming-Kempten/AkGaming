using AKG.Common.Generics;
using Membermanagement.Contracts.DTO;

namespace MemberManagement.Contracts.Services;

public interface IMemberCreationService {
    Task<Result> CreateMemberAsync(MemberCreationDto memberCreationData);
}