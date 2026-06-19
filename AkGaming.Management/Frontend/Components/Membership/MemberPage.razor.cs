using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Management.Frontend.Components.Membership;

public partial class MemberPage : ComponentBase {
    private enum MemberTab {
        Profile,
        Payments,
        Dues
    }

    [Parameter] public string? UserId { get; set; } = string.Empty;
    
    [Inject] MemberManagementApiClient MemberApi { get; set; } = default!;
    [Inject] AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;

    private bool _loading = true;
    private MemberDto? _member;
    private List<MemberLinkingRequestDto> _linkingRequests = [];
    private List<MembershipApplicationRequestDto> _applicationRequests = [];
    private string? _loadError;
    private MemberTab _activeTab = MemberTab.Profile;
    private bool HasPendingRequest => _applicationRequests.Any(x => !x.IsResolved)
        || _linkingRequests.Any(x => !x.IsResolved);
    private bool IsExistingMember => _member?.Status is MembershipStatus.Member
        or MembershipStatus.HonoraryMember
        or MembershipStatus.SupportingMember
        or MembershipStatus.Suspended
        or MembershipStatus.InTrial
        or MembershipStatus.Applicant;
    private bool CanCreateMembershipRequest => !HasPendingRequest && !IsExistingMember;
    
    Guid _userGuid;

    protected override async Task OnInitializedAsync() {
        _loading = true;
        
        if (UserId is null)
            throw new ArgumentNullException(nameof(UserId));
        
        _userGuid = Guid.Parse(UserId);
        
        var createResult = await MemberApi.CreateMyProfileAsync();
        if (IsUnauthorized(createResult)) {
            Nav.NavigateTo("/authentication/logout", forceLoad: true);
            return;
        }
        if (!createResult.IsSuccess) {
            _loadError = createResult.Error ?? "Profile could not be loaded.";
            _loading = false;
            return;
        }

        var memberResult = await MemberApi.GetMemberByUserGuidAsync(_userGuid);
        if (IsUnauthorized(memberResult)) {
            Nav.NavigateTo("/authentication/logout", forceLoad: true);
            return;
        }
        if (memberResult.IsSuccess)
            _member = memberResult.Value;
        else
            _loadError = memberResult.Error ?? "Profile could not be loaded.";

        await LoadRequestsAsync();
        _loading = false;
    }

    private async Task LoadRequestsAsync() {
        var linkingRequestsResult = await MemberApi.GetAllMemberLinkingRequestsByUserAsync(_userGuid);
        if (IsUnauthorized(linkingRequestsResult)) {
            Nav.NavigateTo("/authentication/logout", forceLoad: true);
            return;
        }
        if (linkingRequestsResult.IsSuccess)
            _linkingRequests = linkingRequestsResult.Value?.ToList() ?? [];

        var applicationRequestsResult = await MemberApi.GetAllMembershipApplicationRequestsByUserAsync(_userGuid);
        if (IsUnauthorized(applicationRequestsResult)) {
            Nav.NavigateTo("/authentication/logout", forceLoad: true);
            return;
        }
        if (applicationRequestsResult.IsSuccess)
            _applicationRequests = applicationRequestsResult.Value?.ToList() ?? [];
    }

    private static bool IsUnauthorized(Result result) =>
        !result.IsSuccess && result.Error?.StartsWith("401 ", StringComparison.Ordinal) == true;

    private void HandleMemberUpdated(MemberDto member) {
        _member = member;
    }

    private async Task HandleRequestSubmittedAsync(MemberDto member) {
        _member = member;
        await LoadRequestsAsync();
        StateHasChanged();
    }
}
