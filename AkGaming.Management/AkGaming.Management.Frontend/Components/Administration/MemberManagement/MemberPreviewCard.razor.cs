using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement;

public partial class MemberPreviewCard : ComponentBase {
    [Parameter] public MemberDto Member { get; set; } = null!;
    [Parameter] public EventCallback<MemberDto> OnSelect { get; set; }
    [Parameter] public bool IsSelected { get; set; }
    
    private void SelectMember() {
        OnSelect.InvokeAsync(Member);
    }
}