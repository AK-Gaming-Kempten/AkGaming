using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Disbursements;

public partial class AllocationApplicationEditDialog : ComponentBase
{
    [Parameter, EditorRequired] public required AllocationApplicationDto Application { get; set; }
    [Parameter] public decimal MaximumAmount { get; set; }
    [Parameter] public bool AllowPaymentMethodSelection { get; set; }
    [Parameter] public IReadOnlyList<PaymentInformationDto> PaymentMethods { get; set; } = [];
    [Parameter] public bool Busy { get; set; }
    [Parameter] public string? Error { get; set; }
    [Parameter] public EventCallback OnRefreshPaymentMethods { get; set; }
    [Parameter] public EventCallback<UpdateAllocationApplicationRequest> OnSubmit { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private UpdateAllocationApplicationRequest _model = new();
    private bool _initialized;

    protected override void OnParametersSet()
    {
        if (_initialized)
            return;

        _initialized = true;
        _model = new UpdateAllocationApplicationRequest
        {
            Amount = Application.Amount,
            Note = Application.Note,
            PaymentInformationId = AllowPaymentMethodSelection
                ? Application.PaymentMethod.PaymentInformationId
                : null
        };
    }

    private Guid PaymentInformationId
    {
        get => _model.PaymentInformationId ?? Guid.Empty;
        set => _model.PaymentInformationId = value;
    }

    private bool AmountChanged => _model.Amount != Application.Amount;
    private bool CanSubmit => _model.Amount > 0
        && _model.Amount <= MaximumAmount
        && (!AllowPaymentMethodSelection || PaymentInformationId != Guid.Empty);

    private Task SubmitAsync()
    {
        return OnSubmit.InvokeAsync(_model);
    }
}
