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
        PostJsonAsync($"members/memberLinkingRequests/{requestId}/markResolved",new {}, ct);
    
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
}
