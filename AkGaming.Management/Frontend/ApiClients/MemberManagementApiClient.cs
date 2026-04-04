using System.Text.Json;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;

namespace AkGaming.Management.Frontend.ApiClients;

public sealed class MemberManagementApiClient : ApiClientBase {
    public MemberManagementApiClient(HttpClient http) : base(http) { }

    // ---- Health/Auth --------------------------------------------------------
    public Task<Result<string>> TestAuth(CancellationToken ct = default) =>
        GetAsync<string>("/test-auth", ct);

    // ---- Members ------------------------------------------------------------
    public Task<Result<MemberDto>> GetMemberByGuidAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<MemberDto>($"members/{id}", ct);

    public Task<Result<MemberDto>> GetMemberByUserGuidAsync(Guid userId, CancellationToken ct = default) =>
        GetAsync<MemberDto>($"members/user/{userId}", ct);

    public Task<Result<ICollection<MemberDto>>> GetAllMembersAsync(CancellationToken ct = default) =>
        GetAsync<ICollection<MemberDto>>("members", ct);

    public Task<Result<ICollection<MemberDto>>> GetMembersWithStatusAsync(MembershipStatus status, CancellationToken ct = default) =>
        GetAsync<ICollection<MemberDto>>($"members/byStatus/{status}", ct);

    public Task<Result<ICollection<MemberDto>>> GetMembersWithStatusAsync(ICollection<MembershipStatus> statuses, CancellationToken ct = default) {
        var q = string.Join(",", statuses);
        return GetAsync<ICollection<MemberDto>>($"members?status={q}", ct);
    }

    public Task<Result<Guid>> CreateMemberAsync(MemberCreationDto dto, CancellationToken ct = default) =>
        PostJsonAsync<MemberCreationDto, Guid>("members", dto, ct);

    public Task<Result> UpdateMemberAsync(MemberDto dto, CancellationToken ct = default) =>
        PutJsonAsync("members/" + dto.Id, dto, ct);

    // ---- User linking -------------------------------------------------------
    public Task<Result> LinkMemberToUserAsync(Guid userId, Guid memberId, CancellationToken ct = default) =>
        PostJsonAsync($"members/{memberId}/linkToUser", userId, ct);

    public Task<Result> UnlinkMemberFromUserAsync(Guid userId, Guid memberId, CancellationToken ct = default) =>
        PostJsonAsync($"members/{memberId}/unlinkFromUser", userId, ct);
    
    public Task<Result> SendMemberLinkingRequestAsync(MemberLinkingRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync($"members/memberLinkingRequests", request, ct);
    
    public Task<Result> MarkMemberLinkingRequestResolvedAsync(Guid requestId, CancellationToken ct = default) =>
        PostJsonAsync($"members/memberLinkingRequests/{requestId}/markResolved", new { }, ct);

    public Task<Result> AcceptMemberLinkingRequestAsync(Guid requestId, CancellationToken ct = default) =>
        PostJsonAsync($"members/memberLinkingRequests/{requestId}/accept", new { }, ct);

    public Task<Result> RejectMemberLinkingRequestAsync(Guid requestId, CancellationToken ct = default) =>
        PostJsonAsync($"members/memberLinkingRequests/{requestId}/reject", new { }, ct);
    
    public Task<Result<ICollection<MemberLinkingRequestDto>>> GetAllMemberLinkingRequestAsync(CancellationToken ct = default) =>
        GetAsync<ICollection<MemberLinkingRequestDto>>("members/memberLinkingRequests", ct);
    
    public Task<Result<ICollection<MemberLinkingRequestDto>>> GetAllMemberLinkingRequestsByUserAsync(Guid userId, CancellationToken ct = default) =>
        GetAsync<ICollection<MemberLinkingRequestDto>>($"members/{userId}/memberLinkingRequests/", ct);

    // ---- Membership lifecycle ----------------------------------------------
    public Task<Result> ApplyForMembershipAsync(MembershipApplicationRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync($"members/applyForMembership", request, ct);

    public Task<Result> UpdateMembershipStatusAsync(Guid memberId, MembershipStatus status, CancellationToken ct = default) =>
        PutValueAsync($"members/{memberId}/updateStatus", status, ct);

    public Task<Result> InsertMembershipStatusChangeAsync(Guid memberId, MembershipStatusChangeEventDto changeEvent, CancellationToken ct = default) =>
        PutJsonAsync($"members/{memberId}/insertStatusChangeEvent", changeEvent, ct);
    
    public Task<Result> AcceptMembershipApplicationAsync(Guid requestId, CancellationToken ct = default) =>
        PostJsonAsync($"members/membershipApplicationRequests/{requestId}/accept", new {}, ct);
    
    public Task<Result> RejectMembershipApplicationAsync(Guid requestId, CancellationToken ct = default) =>
        PostJsonAsync($"members/membershipApplicationRequests/{requestId}/reject", new {}, ct);
    
    public Task<Result<ICollection<MembershipApplicationRequestDto>>> GetAllMembershipApplicationRequestsAsync(CancellationToken ct = default) =>
        GetAsync<ICollection<MembershipApplicationRequestDto>>("members/membershipApplicationRequests", ct);

    public Task<Result<ICollection<MembershipApplicationRequestDto>>> GetAllMembershipApplicationRequestsByUserAsync(Guid userId, CancellationToken ct = default) =>
        GetAsync<ICollection<MembershipApplicationRequestDto>>($"members/{userId}/membershipApplicationRequests/", ct);

    public Task<Result<ICollection<MembershipStatusChangeEventDto>>> GetMembershipStatusChangesAsync(Guid memberId, CancellationToken ct = default) =>
        GetAsync<ICollection<MembershipStatusChangeEventDto>>($"members/{memberId}/statusChanges", ct);

    public Task<Result<DateTime>> GetDefaultEndOfTrialPeriodAsync(Guid memberId, CancellationToken ct = default) =>
        GetAsync<DateTime>($"members/{memberId}/endOfTrial", ct);

    public Task<Result<MemberAuditLogsResponseDto>> GetMemberAuditLogsAsync(
        int page = 1,
        int pageSize = 25,
        string? search = null,
        CancellationToken ct = default) {
        var queryParts = new List<string> {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search)) {
            queryParts.Add($"search={Uri.EscapeDataString(search.Trim())}");
        }

        return GetAsync<MemberAuditLogsResponseDto>($"members/audit-logs?{string.Join("&", queryParts)}", ct);
    }

    // ---- Membership dues ---------------------------------------------------
    public Task<Result<MembershipPaymentPeriodDto>> CreatePaymentPeriodAsync(MembershipPaymentPeriodCreateDto request, CancellationToken ct = default) =>
        PostJsonAsync<MembershipPaymentPeriodCreateDto, MembershipPaymentPeriodDto>("membership-dues/payment-periods", request, ct);

    public Task<Result<ICollection<MembershipPaymentPeriodDto>>> GetPaymentPeriodsAsync(CancellationToken ct = default) =>
        GetAsync<ICollection<MembershipPaymentPeriodDto>>("membership-dues/payment-periods", ct);

    public Task<Result<ICollection<MembershipDueDto>>> GetCurrentPaymentPeriodDuesAsync(CancellationToken ct = default) =>
        GetAsync<ICollection<MembershipDueDto>>("membership-dues/payment-periods/current", ct);

    public Task<Result<ICollection<MembershipDueDto>>> GetPaymentPeriodDuesAsync(int paymentPeriodId, CancellationToken ct = default) =>
        GetAsync<ICollection<MembershipDueDto>>($"membership-dues/payment-periods/{paymentPeriodId}", ct);

    public Task<Result<MembershipDueReminderDispatchPreviewDto>> GetReminderDispatchPreviewForPaymentPeriodAsync(int paymentPeriodId, CancellationToken ct = default) =>
        GetAsync<MembershipDueReminderDispatchPreviewDto>($"membership-dues/payment-periods/{paymentPeriodId}/reminder-dispatch", ct);

    public Task<Result<ICollection<MembershipDueDto>>> AddMembersToPaymentPeriodAsync(int paymentPeriodId, ICollection<Guid> memberIds, CancellationToken ct = default) =>
        PostJsonAsync<ICollection<Guid>, ICollection<MembershipDueDto>>($"membership-dues/payment-periods/{paymentPeriodId}/members", memberIds, ct);

    public Task<Result<ICollection<MembershipDueDto>>> GetDuesForMemberAsync(Guid memberId, CancellationToken ct = default) =>
        GetAsync<ICollection<MembershipDueDto>>($"membership-dues/members/{memberId}", ct);

    public Task<Result<ICollection<MembershipDueDto>>> GetMyDuesAsync(CancellationToken ct = default) =>
        GetAsync<ICollection<MembershipDueDto>>("membership-dues/me", ct);

    public Task<Result<MembershipDueEmailPreviewDto>> GetReminderEmailPreviewAsync(int dueId, CancellationToken ct = default) =>
        GetAsync<MembershipDueEmailPreviewDto>($"membership-dues/{dueId}/reminder-email", ct);

    public Task<Result<MembershipDueReminderDispatchPreviewDto>> GetReminderDispatchPreviewForDueAsync(int dueId, CancellationToken ct = default) =>
        GetAsync<MembershipDueReminderDispatchPreviewDto>($"membership-dues/{dueId}/reminder-dispatch", ct);

    public Task<Result> SendReminderEmailAsync(int dueId, CancellationToken ct = default) =>
        PostJsonAsync($"membership-dues/{dueId}/send-reminder", new { }, ct);

    public Task<Result> UpdateDueAsync(int dueId, MembershipDueDto due, CancellationToken ct = default) =>
        PutJsonAsync($"membership-dues/{dueId}", due, ct);
}
