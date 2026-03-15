using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;

namespace AkGaming.Management.Modules.MemberManagement.Application.Interfaces;

public interface IMembershipPaymentPeriodRepository {
    /// <summary>
    /// Gets all payment periods sorted by creation date descending.
    /// </summary>
    Task<Result<List<MembershipPaymentPeriod>>> GetAllAsync();

    /// <summary>
    /// Gets a payment period by its identifier.
    /// </summary>
    Task<Result<MembershipPaymentPeriod>> GetByIdAsync(int id);

    /// <summary>
    /// Gets the currently active payment period.
    /// </summary>
    Task<Result<MembershipPaymentPeriod>> GetCurrentAsync();

    /// <summary>
    /// Adds a payment period to the repository.
    /// </summary>
    Result Add(MembershipPaymentPeriod paymentPeriod);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    Task<Result> SaveChangesAsync();
}
