using System.Text.Json;
using Frontend.Blazor.ApiClients;
using MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace Frontend.Blazor.Components.Administration.MemberManagement;

public partial class MemberCard : ComponentBase {
    [CascadingParameter(Name = "MemberManagementApi")]
    public MemberManagementApiClient Api { get; set; } = default!;
    [Parameter] public MemberDto Member { get; set; } = default!;
    [Parameter] public bool IsEditable { get; set; } = false;
    [Parameter] public bool IsStatusEditable { get; set; } = false;
    [Parameter] public EventCallback<MemberDto> OnMemberUpdated { get; set; }
    
    protected bool EditMode { get; set; } = false;
    protected MemberDto _localMember = new();

    protected override void OnParametersSet() {
        _localMember = JsonSerializer.Deserialize<MemberDto>(JsonSerializer.Serialize(Member))!;
        if( _localMember != null && _localMember.Address == null)
        _localMember.Address = new AddressDto();
    }

    private void EnableEditing() => EditMode = true;

    private async Task SaveChanges() {
        await Api.UpdateMemberAsync(_localMember);
        EditMode = false;
        await OnMemberUpdated.InvokeAsync(_localMember);
    }

    private void CancelChanges() {
        _localMember = JsonSerializer.Deserialize<MemberDto>(JsonSerializer.Serialize(Member))!;
        if( _localMember != null && _localMember.Address == null)
            _localMember.Address = new AddressDto();
        EditMode = false;
    }
    
    private void StatusUpdated(MemberDto member) {
        OnMemberUpdated.InvokeAsync(member);
    }
}