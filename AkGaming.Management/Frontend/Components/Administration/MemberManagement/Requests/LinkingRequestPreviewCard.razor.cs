using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement.Requests;

public partial class LinkingRequestPreviewCard : ComponentBase {
    [Parameter] public MemberLinkingRequestDto Request { get; set; } = null!;
    [Parameter] public EventCallback<MemberLinkingRequestDto> OnSelect { get; set; }
    [Parameter] public bool IsSelected { get; set; }
    
    private void SelectRequest() {
        OnSelect.InvokeAsync(Request);
    }
}