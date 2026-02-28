using MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace Frontend.Blazor.Components.Administration.MemberManagement;

public partial class MemberPreviewCard : ComponentBase {
    [Parameter] public MemberDto Member { get; set; } = null!;
    [Parameter] public EventCallback<MemberDto> OnSelect { get; set; }
    [Parameter] public bool IsSelected { get; set; }
    
    private void SelectMember() {
        OnSelect.InvokeAsync(Member);
    }
}