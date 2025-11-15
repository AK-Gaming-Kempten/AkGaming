using Frontend.Blazor.ApiClients;
using MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace Frontend.Blazor.Components.Membership;

public partial class ApplyForMembershipPanel : ComponentBase {
    [Inject] 
    private MemberManagementApiClient MemberApi { get; set; } = default!;
    [Inject] 
    private NavigationManager Nav { get; set; } = default!;
    
    [Parameter] 
    public Guid UserGuid { get; set; } = Guid.Empty;
    
    private readonly MembershipApplicationRequestDto _application = new() {
        MemberCreationInfo = new MemberCreationDto() {
            Address = new AddressDto()
        }
    };
    private string? _createError;

    private async Task ApplyForMembershipAsync() {
        _createError = null;
        if(UserGuid == Guid.Empty) {
            _createError = "Can not apply for membership without a valid user ID!";
            return;
        }
        
        _application.IssuingUserId = UserGuid;
        
        try {
            var response = await MemberApi.ApplyForMembershipAsync(_application);
            if (response.IsSuccess) {
                Nav.NavigateTo($"/membership");
            }
        }
        catch (Exception ex) {
            _createError = ex.Message;
        }
    }
}