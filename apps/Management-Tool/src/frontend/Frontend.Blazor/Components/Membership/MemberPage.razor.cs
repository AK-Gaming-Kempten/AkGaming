using Frontend.Blazor.ApiClients;
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
    Guid _userGuid;

    protected override async Task OnInitializedAsync() {
        _loading = true;
        
        if (UserId is null)
            throw new ArgumentNullException(nameof(UserId));
        
        _userGuid = Guid.Parse(UserId);
        
        var result = await MemberApi.GetMemberByUserGuidAsync(_userGuid);

        if (result.IsSuccess)
            _member = result.Value;

        _loading = false;
    }
}