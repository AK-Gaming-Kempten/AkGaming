using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement.Requests;

public partial class MemberManagementRequestsPage : ComponentBase {
    private enum RequestTab {
        LinkingRequests,
        ApplicationRequests
    }
    
    [Inject] 
    private MemberManagementApiClient MemberApi { get; set; } = default!;
    private RequestTab _activeTab = RequestTab.ApplicationRequests;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    private bool _canManageRequests;

    protected override async Task OnInitializedAsync() {
        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _canManageRequests = authenticationState.User.HasClaim("permission", "management.requests.manage");
    }
}
