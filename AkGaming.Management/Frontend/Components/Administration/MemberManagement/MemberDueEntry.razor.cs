using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement;

public partial class MemberDueEntry : ComponentBase {
    [Parameter] public required MembershipDueDto Due { get; set; }
    [Parameter] public string PrimaryMemberLabel { get; set; } = string.Empty;
    [Parameter] public string? SecondaryMemberLabel { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public EventCallback<MembershipDueDto> OnSave { get; set; }

    private readonly MembershipDueStatus[] _statuses = Enum.GetValues<MembershipDueStatus>();
    private MembershipDueDto _editingDue = new();
    private bool EditMode { get; set; }

    protected override void OnParametersSet() {
        if (!EditMode) {
            _editingDue = CloneDue(Due);
        }
    }

    private void ToggleEditMode() {
        EditMode = !EditMode;
        if (!EditMode) {
            _editingDue = CloneDue(Due);
        }
    }

    private void CancelEditing() {
        _editingDue = CloneDue(Due);
        EditMode = false;
    }

    private async Task SaveDueAsync() {
        await OnSave.InvokeAsync(_editingDue);
        EditMode = false;
    }

    private static MembershipDueDto CloneDue(MembershipDueDto due) => new() {
        Id = due.Id,
        PaymentPeriodId = due.PaymentPeriodId,
        MemberId = due.MemberId,
        Status = due.Status,
        DueAmount = due.DueAmount,
        PaidAmount = due.PaidAmount,
        DueDate = due.DueDate,
        SettledAt = due.SettledAt,
        SettlementReference = due.SettlementReference
    };

    private static string GetSettledAtValue(DateTimeOffset? settledAt) =>
        settledAt?.ToLocalTime().ToString("yyyy-MM-ddTHH:mm") ?? string.Empty;

    private void SetSettledAt(ChangeEventArgs args) {
        var value = args.Value?.ToString();
        if (string.IsNullOrWhiteSpace(value)) {
            _editingDue.SettledAt = null;
            return;
        }

        if (DateTime.TryParse(value, out var parsedDateTime)) {
            _editingDue.SettledAt = new DateTimeOffset(parsedDateTime);
        }
    }

    private static string GetStatusClass(MembershipDueStatus status) => status switch {
        MembershipDueStatus.Pending => "due-status-open",
        MembershipDueStatus.Paid => "due-status-paid",
        MembershipDueStatus.Waived => "due-status-waived",
        MembershipDueStatus.Cancelled => "due-status-cancelled",
        _ => string.Empty
    };
}
