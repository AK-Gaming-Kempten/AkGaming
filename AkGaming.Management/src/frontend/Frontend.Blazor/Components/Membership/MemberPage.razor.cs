using Frontend.Blazor.ApiClients;
using AKG.Common.Generics;
using MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Frontend.Blazor.Components.Membership;

public partial class MemberPage : ComponentBase {
    [Parameter] public string? UserId { get; set; } = string.Empty;
    
    [Inject] MemberManagementApiClient MemberApi { get; set; } = default!;
    [Inject] AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;

    private bool _loading = true;
    private MemberDto? _member;
    private MemberLinkingRequestDto? _linkingRequest;
    private MembershipApplicationRequestDto? _applicationRequest;
    
    Guid _userGuid;

    protected override async Task OnInitializedAsync() {
        _loading = true;
        
        if (UserId is null)
            throw new ArgumentNullException(nameof(UserId));
        
        _userGuid = Guid.Parse(UserId);
        
        var memberResult = await MemberApi.GetMemberByUserGuidAsync(_userGuid);
        if (IsUnauthorized(memberResult)) {
            Nav.NavigateTo("/authentication/logout", forceLoad: true);
            return;
        }
        if (memberResult.IsSuccess)
            _member = memberResult.Value;

        var linkingRequestsResult = await MemberApi.GetAllMemberLinkingRequestsByUserAsync(_userGuid);
        if (IsUnauthorized(linkingRequestsResult)) {
            Nav.NavigateTo("/authentication/logout", forceLoad: true);
            return;
        }
        if (linkingRequestsResult.IsSuccess)
            _linkingRequest = linkingRequestsResult.Value!.FirstOrDefault(x => !x.IsResolved);

        var applicationRequestsResult = await MemberApi.GetAllMembershipApplicationRequestsByUserAsync(_userGuid);
        if (IsUnauthorized(applicationRequestsResult)) {
            Nav.NavigateTo("/authentication/logout", forceLoad: true);
            return;
        }
        if (applicationRequestsResult.IsSuccess)
            _applicationRequest = applicationRequestsResult.Value!.FirstOrDefault(x => !x.IsResolved);

        _loading = false;
    }

    private static bool IsUnauthorized(Result result) =>
        !result.IsSuccess && result.Error?.StartsWith("401 ", StringComparison.Ordinal) == true;
}
