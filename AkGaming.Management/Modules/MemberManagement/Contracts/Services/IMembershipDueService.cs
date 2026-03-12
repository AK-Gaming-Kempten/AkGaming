using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

namespace AkGaming.Management.Modules.MemberManagement.Contracts.Services;

public interface IMembershipDueService {
    /// <summary>
    /// Creates a new payment period and generates dues for all current members.
    /// </summary>
    Task<Result<MembershipPaymentPeriodDto>> CreatePaymentPeriodAsync(MembershipPaymentPeriodCreateDto request, Guid? performedByUserId = null);

    /// <summary>
    /// Returns dues for the active payment period.
    /// </summary>
    Task<Result<ICollection<MembershipDueDto>>> GetCurrentPaymentPeriodDuesAsync();

    /// <summary>
    /// Returns dues for a specific payment period.
    /// </summary>
    Task<Result<ICollection<MembershipDueDto>>> GetPaymentPeriodDuesAsync(int paymentPeriodId);

    /// <summary>
    /// Adds one or more members as dues to an existing payment period.
    /// </summary>
    Task<Result<ICollection<MembershipDueDto>>> AddMembersToPaymentPeriodAsync(int paymentPeriodId, ICollection<Guid> memberIds, Guid? performedByUserId = null);

    /// <summary>
    /// Returns all dues associated with a member.
    /// </summary>
    Task<Result<ICollection<MembershipDueDto>>> GetDuesForMemberAsync(Guid memberId);

    /// <summary>
    /// Updates an existing membership due.
    /// </summary>
    Task<Result> UpdateDueAsync(int dueId, MembershipDueDto due, Guid? performedByUserId = null);
}
