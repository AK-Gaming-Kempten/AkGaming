using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Membership;

public partial class NoMemberPanel : ComponentBase {
    private enum FormMode {
        None,
        RequestLink,
        Apply,
    }

    private FormMode _formMode = FormMode.None;
    
    [Parameter] public Guid UserId { get; set; }
    [Parameter] public MemberLinkingRequestDto? PendingLinkingRequest { get; set; }
    [Parameter] public MembershipApplicationRequestDto? PendingApplication { get; set; }

    [Inject] NavigationManager Nav { get; set; } = default!;
    
    private void ShowRequestLink() => _formMode = FormMode.RequestLink;
    private void ShowApply()       => _formMode = FormMode.Apply;
}