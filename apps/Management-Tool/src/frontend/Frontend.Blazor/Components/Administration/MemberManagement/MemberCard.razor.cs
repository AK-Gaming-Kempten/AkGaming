using System.Text.Json;
using MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace Frontend.Blazor.Components.Administration.MemberManagement;

public partial class MemberCard : ComponentBase {
    [Parameter] public MemberDto Member { get; set; } = default!;
    [Parameter] public bool IsEditable { get; set; } = false;

    [Parameter] public EventCallback<MemberDto> OnSave { get; set; }
    [Parameter] public EventCallback<MembershipStatusChangeEventDto> OnStatusChange { get; set; }

    protected bool EditMode { get; set; } = false;
    protected MemberDto _localMember = new();

    protected override void OnParametersSet() {
        _localMember = JsonSerializer.Deserialize<MemberDto>(JsonSerializer.Serialize(Member))!;
    }

    private void EnableEditing() => EditMode = true;

    private async Task SaveChanges() {
        if (OnSave.HasDelegate)
            await OnSave.InvokeAsync(_localMember);

        EditMode = false;
    }

    private void CancelChanges() {
        _localMember = JsonSerializer.Deserialize<MemberDto>(JsonSerializer.Serialize(Member))!;
        EditMode = false;
    }

    private async Task SubmitStatusChange(MembershipStatusChangeEventDto dto) {
        if (OnStatusChange.HasDelegate)
            await OnStatusChange.InvokeAsync(dto);
    }
}