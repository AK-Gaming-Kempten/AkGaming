using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.Disbursements.Contracts.Enums;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Disbursements;

public partial class ReimbursementCard : ComponentBase
{
    [Parameter, EditorRequired] public required ReimbursementDto Item { get; set; }
    [Parameter] public bool Administrative { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public EventCallback<ReceiptDto> OnDownload { get; set; }
    [Parameter] public EventCallback<UpdateReimbursementStatusRequest> OnStatusChanged { get; set; }
    [Parameter] public EventCallback<Guid> OnCancel { get; set; }
    private DisbursementStatus _status;
    private string? _administrativeNote;
    private bool _showPaymentDetails;

    protected override void OnParametersSet()
    {
        _status = Item.Status;
        _administrativeNote = Item.AdministrativeNote;
    }

    private Task SaveStatusAsync() => OnStatusChanged.InvokeAsync(new UpdateReimbursementStatusRequest { Status = _status, AdministrativeNote = _administrativeNote });
    private static string FormatStatus(DisbursementStatus value) => value switch { DisbursementStatus.UnderReview => "Under review", _ => value.ToString() };
}
