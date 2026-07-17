using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Disbursements;

public partial class PaymentMethodDetailsDialog : ComponentBase
{
    [Parameter, EditorRequired] public required PaymentMethodSnapshotDto PaymentMethod { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
}
