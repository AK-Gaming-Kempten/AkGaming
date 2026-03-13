using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;

namespace AkGaming.Management.Modules.MemberManagement.Application.Interfaces;

public interface IMembershipPaymentPeriodRepository {
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
