using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement;

public partial class PaymentPeriodCreateDialog : ComponentBase {
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public string? Error { get; set; }
    [Parameter] public EventCallback<MembershipPaymentPeriodCreateDto> OnSubmit { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private readonly MembershipPaymentPeriodCreateDto _request = CreateDefaultRequest();

    private async Task SubmitAsync() {
        await OnSubmit.InvokeAsync(_request);
    }

    private static MembershipPaymentPeriodCreateDto CreateDefaultRequest() {
        var today = DateTime.UtcNow.Date;
        return new MembershipPaymentPeriodCreateDto {
            Name = $"{today:yyyy-MM}",
            DueDate = DateOnly.FromDateTime(today),
            DefaultDueAmount = 10m
        };
    }
}
