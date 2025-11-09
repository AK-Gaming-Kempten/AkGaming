using Frontend.Blazor.ApiClients;
using MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace Frontend.Blazor.Components.Membership;

public partial class RequestMemberLinkingPanel : ComponentBase {
    [Inject] 
    private MemberManagementApiClient MemberApi { get; set; } = default!;
    [Inject] 
    private NavigationManager Nav { get; set; } = default!;
    
    [Parameter] 
    public Guid UserGuid { get; set; } = Guid.Empty;
    
    private MemberLinkingRequestDto _request = new();
    private string? _requestError;

    private async Task RequestMemberLinkingAsync() {
        _requestError = "Linking service not implemented yet!";
    }
}