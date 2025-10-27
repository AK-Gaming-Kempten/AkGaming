using AKG.Common.Generics;
using MemberManagement.Contracts.DTO;

namespace MemberManagement.Contracts.Services;

public interface IMemberCreationService {
    Task<Result> CreateMemberAsync(MemberCreationDto memberCreationData);
}