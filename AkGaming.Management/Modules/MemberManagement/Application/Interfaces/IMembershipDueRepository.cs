using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;

namespace AkGaming.Management.Modules.MemberManagement.Application.Interfaces;

public interface IMembershipDueRepository {
    /// <summary>
    /// Gets a due entry by its identifier.
    /// </summary>
    Task<Result<MembershipDue>> GetByIdAsync(int id);

    /// <summary>
    /// Gets all dues linked to a payment period.
    /// </summary>
    Task<Result<List<MembershipDue>>> GetByPaymentPeriodIdAsync(int paymentPeriodId);

    /// <summary>
    /// Gets all dues linked to a member.
    /// </summary>
    Task<Result<List<MembershipDue>>> GetByMemberIdAsync(Guid memberId);

    /// <summary>
    /// Adds a due entry to the repository.
    /// </summary>
    Result Add(MembershipDue due);

    /// <summary>
    /// Adds multiple due entries to the repository.
    /// </summary>
    Result AddRange(ICollection<MembershipDue> dues);

    /// <summary>
    /// Marks a due entry as updated.
    /// </summary>
    Result Update(MembershipDue due);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    Task<Result> SaveChangesAsync();
}
