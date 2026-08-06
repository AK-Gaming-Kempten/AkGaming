using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Disbursements;

public partial class AllocationApprovalList : ComponentBase
{
    [Parameter, EditorRequired]
    public required IReadOnlyList<AllocationApprovalDto> Approvals { get; set; }
}
