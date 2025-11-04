using MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace Frontend.Blazor.Components.Membership;

public partial class MemberInfoPanel : ComponentBase {
    [Parameter] public MemberDto Member { get; set; } = default!;
}