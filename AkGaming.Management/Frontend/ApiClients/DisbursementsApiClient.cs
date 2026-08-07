using System.Net.Http.Json;
using System.Text.Json;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;

namespace AkGaming.Management.Frontend.ApiClients;

public sealed class DisbursementsApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<Result<IReadOnlyList<ReimbursementDto>>> GetMyReimbursementsAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyList<ReimbursementDto>>("disbursements/reimbursements/me", cancellationToken);
    public Task<Result<IReadOnlyList<ReimbursementDto>>> GetAllReimbursementsAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyList<ReimbursementDto>>("disbursements/admin/reimbursements", cancellationToken);
    public Task<Result<ReimbursementDto>> CancelMyReimbursementAsync(Guid id, CancellationToken cancellationToken = default) => PostJsonAsync<object, ReimbursementDto>($"disbursements/reimbursements/me/{id}/cancel", new { }, cancellationToken);
    public Task<Result<ReimbursementDto>> UpdateReimbursementStatusAsync(Guid id, UpdateReimbursementStatusRequest request, CancellationToken cancellationToken = default) => PutJsonAsync<UpdateReimbursementStatusRequest, ReimbursementDto>($"disbursements/admin/reimbursements/{id}/status", request, cancellationToken);
    public Task<Result<IReadOnlyList<DisbursementEventDto>>> GetEventsAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyList<DisbursementEventDto>>("disbursements/admin/events", cancellationToken);
    public Task<Result<DisbursementEventDto>> GetEventAsync(Guid id, CancellationToken cancellationToken = default) => GetAsync<DisbursementEventDto>($"disbursements/admin/events/{id}", cancellationToken);
    public Task<Result<DisbursementEventDto>> CreateEventAsync(SaveDisbursementEventRequest request, CancellationToken cancellationToken = default) => PostJsonAsync<SaveDisbursementEventRequest, DisbursementEventDto>("disbursements/admin/events", request, cancellationToken);
    public Task<Result<AllocationDto>> CreateAllocationAsync(Guid eventId, SaveAllocationRequest request, CancellationToken cancellationToken = default) => PostJsonAsync<SaveAllocationRequest, AllocationDto>($"disbursements/admin/events/{eventId}/allocations", request, cancellationToken);
    public Task<Result<DiscordGuildCatalogDto>> GetDiscordCatalogAsync(CancellationToken cancellationToken = default) => GetAsync<DiscordGuildCatalogDto>("disbursements/admin/discord/catalog", cancellationToken);
    public Task<Result<AllocationDto>> GetAllocationAsync(Guid token, CancellationToken cancellationToken = default) => GetAsync<AllocationDto>($"disbursements/allocations/share/{token}", cancellationToken);
    public Task<Result<IReadOnlyList<AllocationDto>>> GetMyAllocationsAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyList<AllocationDto>>("disbursements/allocations/me", cancellationToken);
    public Task<Result<AllocationApplicationDto>> ApplyAsync(Guid token, CreateAllocationApplicationRequest request, CancellationToken cancellationToken = default) => PostJsonAsync<CreateAllocationApplicationRequest, AllocationApplicationDto>($"disbursements/allocations/share/{token}/applications", request, cancellationToken);
    public Task<Result<AllocationApplicationDto>> DecideAsync(Guid token, Guid applicationId, DecideAllocationApplicationRequest request, CancellationToken cancellationToken = default) => PutJsonAsync<DecideAllocationApplicationRequest, AllocationApplicationDto>($"disbursements/allocations/share/{token}/applications/{applicationId}/decision", request, cancellationToken);
    public Task<Result<AllocationApplicationDto>> UpdateApplicationStatusAsync(Guid applicationId, UpdateAllocationApplicationStatusRequest request, CancellationToken cancellationToken = default) => PutJsonAsync<UpdateAllocationApplicationStatusRequest, AllocationApplicationDto>($"disbursements/admin/applications/{applicationId}/status", request, cancellationToken);
    public Task<Result<byte[]>> DownloadReceiptAsync(Guid receiptId, bool administrative, CancellationToken cancellationToken = default) => GetBytesAsync(administrative ? $"disbursements/admin/receipts/{receiptId}" : $"disbursements/receipts/{receiptId}", cancellationToken);

    public async Task<Result<ReimbursementDto>> CreateReimbursementAsync(CreateReimbursementRequest request, IReadOnlyList<ReceiptUploadFile> files, CancellationToken cancellationToken = default)
    {
        try
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(JsonSerializer.Serialize(request, Json)), "requestJson");
            foreach (var file in files)
            {
                var content = new ByteArrayContent(file.Content);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                form.Add(content, "receipts", file.FileName);
            }
            using var response = await Http.PostAsync("disbursements/reimbursements", form, cancellationToken);
            return await ToResult<ReimbursementDto>(response, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or HttpRequestException)
        {
            return Result<ReimbursementDto>.Failure(exception.Message);
        }
    }
}
