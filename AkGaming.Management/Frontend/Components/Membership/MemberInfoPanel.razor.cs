using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Membership;

public partial class MemberInfoPanel : ComponentBase {
    [Parameter] public MemberDto Member { get; set; } = default!;
}