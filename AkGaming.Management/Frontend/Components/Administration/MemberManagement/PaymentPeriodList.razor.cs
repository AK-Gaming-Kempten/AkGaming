using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement;

public partial class PaymentPeriodList : ComponentBase
{
    [Parameter, EditorRequired] public required IReadOnlyList<MembershipPaymentPeriodDto> Periods { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<MembershipPaymentPeriodDto> OnSelect { get; set; }

    private Task SelectAsync(MembershipPaymentPeriodDto period)
    {
        return OnSelect.InvokeAsync(period);
    }
}
