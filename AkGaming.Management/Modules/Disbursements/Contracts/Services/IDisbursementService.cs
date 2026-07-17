using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;

namespace AkGaming.Management.Modules.Disbursements.Contracts.Services;

public interface IDisbursementService
{
    Task<Result<IReadOnlyList<ReimbursementDto>>> GetReimbursementsAsync(Guid? userId, CancellationToken cancellationToken = default);
    Task<Result<ReimbursementDto>> GetReimbursementAsync(Guid id, Guid? ownerUserId, CancellationToken cancellationToken = default);
    Task<Result<ReimbursementDto>> CreateReimbursementAsync(Guid userId, string applicantName, CreateReimbursementRequest request, IReadOnlyList<ReceiptUpload> files, CancellationToken cancellationToken = default);
    Task<Result<ReimbursementDto>> UpdateReimbursementStatusAsync(Guid id, UpdateReimbursementStatusRequest request, CancellationToken cancellationToken = default);
    Task<Result<ReimbursementDto>> CancelReimbursementAsync(Guid id, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<Result<ReceiptDownload>> GetReceiptAsync(Guid receiptId, Guid? ownerUserId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<DisbursementEventDto>>> GetEventsAsync(CancellationToken cancellationToken = default);
    Task<Result<DisbursementEventDto>> GetEventAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<DisbursementEventDto>> CreateEventAsync(SaveDisbursementEventRequest request, CancellationToken cancellationToken = default);
    Task<Result<AllocationDto>> CreateAllocationAsync(Guid eventId, SaveAllocationRequest request, CancellationToken cancellationToken = default);
    Task<Result<AllocationDto>> GetAllocationByTokenAsync(Guid token, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<AllocationDto>>> GetAllocationsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<AllocationApplicationDto>> ApplyAsync(Guid token, Guid userId, string applicantName, CreateAllocationApplicationRequest request, CancellationToken cancellationToken = default);
    Task<Result<AllocationApplicationDto>> DecideAsync(Guid token, Guid applicationId, Guid userId, string approverName, DecideAllocationApplicationRequest request, CancellationToken cancellationToken = default);
    Task<Result<AllocationApplicationDto>> UpdateApplicationStatusAsync(Guid applicationId, UpdateAllocationApplicationStatusRequest request, CancellationToken cancellationToken = default);
}

public sealed record ReceiptUpload(string FileName, string ContentType, long Size, Stream Content);
public sealed record ReceiptDownload(string FileName, string ContentType, Stream Content);
