using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement;

public partial class MemberDueEntry : ComponentBase {
    [Inject] private MemberManagementApiClient MemberApi { get; set; } = default!;

    [Parameter] public required MembershipDueDto Due { get; set; }
    [Parameter] public string PrimaryMemberLabel { get; set; } = string.Empty;
    [Parameter] public string? SecondaryMemberLabel { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public EventCallback<MembershipDueDto> OnSave { get; set; }
    [Parameter] public EventCallback<MembershipDueDto> OnRequestReminderSend { get; set; }

    private readonly MembershipDueStatus[] _statuses = Enum.GetValues<MembershipDueStatus>();
    private MembershipDueDto _editingDue = new();
    private bool EditMode { get; set; }
    private string? _previewCacheKey;
    private MembershipDueEmailPreviewDto? _reminderPreview;
    private bool _showReminderPreview;
    private bool _loadingReminderPreview;
    private string? _reminderPreviewError;

    protected override void OnParametersSet() {
        var previewCacheKey = BuildPreviewCacheKey(Due);
        if (_previewCacheKey != previewCacheKey) {
            _previewCacheKey = previewCacheKey;
            _reminderPreview = null;
            _showReminderPreview = false;
            _loadingReminderPreview = false;
            _reminderPreviewError = null;
        }

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

    private async Task RequestReminderSendAsync() {
        await OnRequestReminderSend.InvokeAsync(Due);
    }

    private bool CanPreviewReminder => Due.Status == MembershipDueStatus.Pending && Due.IsOverdue();

    private MarkupString ReminderHtmlPreview => new(_reminderPreview?.HtmlBody ?? string.Empty);

    private async Task ToggleReminderPreviewAsync() {
        if (_showReminderPreview) {
            _showReminderPreview = false;
            return;
        }

        _showReminderPreview = true;
        if (_reminderPreview is not null || _loadingReminderPreview)
            return;

        _loadingReminderPreview = true;
        _reminderPreviewError = null;

        var result = await MemberApi.GetReminderEmailPreviewAsync(Due.Id);
        if (!result.IsSuccess) {
            _reminderPreviewError = result.Error ?? "Failed to load reminder preview.";
            _loadingReminderPreview = false;
            return;
        }

        _reminderPreview = result.Value;
        _loadingReminderPreview = false;
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

    private static string BuildPreviewCacheKey(MembershipDueDto due) =>
        $"{due.Id}|{due.Status}|{due.DueAmount}|{due.PaidAmount}|{due.DueDate}";

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
