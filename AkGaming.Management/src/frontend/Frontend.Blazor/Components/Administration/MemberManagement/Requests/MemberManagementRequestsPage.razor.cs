using Frontend.Blazor.ApiClients;
using MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace Frontend.Blazor.Components.Administration.MemberManagement.Requests;

public partial class MemberManagementRequestsPage : ComponentBase {
    private enum RequestTab {
        LinkingRequests,
        ApplicationRequests
    }
    
    [Inject] 
    private MemberManagementApiClient MemberApi { get; set; } = default!;
    private RequestTab _activeTab = RequestTab.ApplicationRequests;
}