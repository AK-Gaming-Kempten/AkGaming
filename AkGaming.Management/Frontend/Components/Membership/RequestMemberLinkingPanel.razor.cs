using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Membership;

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
        _requestError = null;
        if(UserGuid == Guid.Empty) {
            _requestError = "Can not apply for membership without a valid user ID!";
            return;
        }
        
        _request.IssuingUserId = UserGuid;
        
        try {
            var response = await MemberApi.SendMemberLinkingRequestAsync(_request);
            if (response.IsSuccess) {
                Nav.NavigateTo($"/membership");
            }
        }
        catch (Exception ex) {
            _requestError = ex.Message;
        }
    }
}