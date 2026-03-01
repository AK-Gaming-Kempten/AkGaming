using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement.Requests;

public partial class MemberManagementRequestsPage : ComponentBase {
    private enum RequestTab {
        LinkingRequests,
        ApplicationRequests
    }
    
    [Inject] 
    private MemberManagementApiClient MemberApi { get; set; } = default!;
    private RequestTab _activeTab = RequestTab.ApplicationRequests;
}