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
    
    private readonly MemberCreationDto _newMember = new() {Address = new AddressDto()};
    private string? _createError;

    private async Task ApplyForMembershipAsync() {
        _createError = null;
        if(UserGuid == Guid.Empty) {
            _createError = "Can not apply for membership without a valid user ID!";
            return;
        }

        try {
            var response = await MemberApi.ApplyForMembershipAsync(UserGuid, _newMember);
            if (response.IsSuccess) {
                Nav.NavigateTo($"/Membership/User/{UserGuid}");
            }
        }
        catch (Exception ex) {
            _createError = ex.Message;
        }
    }
}