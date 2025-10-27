using AKG.Common.Generics;
using MemberManagement.Contracts.DTO;

namespace MemberManagement.Contracts.Services;

public interface IMemberUpdateService {
    /// <summary>
    /// Updates a Member on the database with the given <see cref="MemberDto"/>
    /// </summary>
    /// <param name="memberId"> The id of the Member to update </param>
    /// <param name="memberData"> The <see cref="MemberDto"/> storing the updated Member data </param>
    /// <returns></returns>
    Task<Result> UpdateMemberAsync(Guid memberId, MemberDto memberData);
}