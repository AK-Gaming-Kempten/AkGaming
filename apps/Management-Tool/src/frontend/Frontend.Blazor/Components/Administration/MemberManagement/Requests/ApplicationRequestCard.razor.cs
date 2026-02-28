using System.Text.Json;
using Frontend.Blazor.ApiClients;
using MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace Frontend.Blazor.Components.Administration.MemberManagement.Requests;

public partial class ApplicationRequestCard : ComponentBase {
    [CascadingParameter(Name = "MemberManagementApi")]
    public MemberManagementApiClient MemberApi { get; set; } = default!;
    [Parameter] public MembershipApplicationRequestDto? Request { get; set; }
    [Parameter] public bool IsEditable { get; set; } = false;
    [Parameter] public EventCallback<MembershipApplicationRequestDto> OnRequestUpdated { get; set; }
    
    private async Task Approve() {
        if (Request == null)
            return;
        await MemberApi.AcceptMembershipApplicationAsync(Request!.Id);
        await OnRequestUpdated.InvokeAsync(Request);
    }
    
    private async Task Reject() {
        if (Request == null)
            return;
        await MemberApi.RejectMembershipApplicationAsync(Request!.Id);
        await OnRequestUpdated.InvokeAsync(Request);
    }
}