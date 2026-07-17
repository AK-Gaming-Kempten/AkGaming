using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Disbursements;

public partial class PaymentMethodSelector : ComponentBase
{
    [Parameter] public IReadOnlyList<PaymentInformationDto> Methods { get; set; } = [];
    [Parameter] public Guid Value { get; set; }
    [Parameter] public EventCallback<Guid> ValueChanged { get; set; }
    [Parameter] public EventCallback OnRefresh { get; set; }
    [Parameter] public bool Disabled { get; set; }

    private Task SelectAsync(Guid id) => ValueChanged.InvokeAsync(id);
    private static string Describe(PaymentInformationDto method) => method.Type == PaymentInformationType.PayPal
        ? method.PayPalEmail ?? string.Empty
        : $"{method.AccountHolder} · ••••{LastFour(method.Iban)}";
    private static string LastFour(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Length <= 4 ? value : value[^4..];
}
