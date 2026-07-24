using AkGaming.Core.Common.Generics;
using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement;

public partial class MemberManagementDuesPage : ComponentBase {
    [Inject] private MemberManagementApiClient MemberApi { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private MembershipPaymentPeriodCreateDto _createRequest = new() {
        Name = $"{DateTime.UtcNow:yyyy-MM}",
        DueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
        DefaultDueAmount = 10m
    };

    private readonly List<MembershipPaymentPeriodDto> _paymentPeriods = [];
    private readonly List<MembershipDueDto> _dues = [];
    private readonly Dictionary<Guid, string> _memberFullNameLookup = [];
    private readonly HashSet<int> _savingDueIds = [];
    private List<ReminderDispatchRecipientState> _dispatchRecipientStates = [];
    private int? _selectedPaymentPeriodId;
    private MemberDisplayMode _memberDisplayMode = MemberDisplayMode.FullName;
    private string? _memberNameFilter;
    private MembershipDueStatus? _selectedStatusFilter;

    private bool _loadingPaymentPeriods;
    private bool _loadingDues;
    private bool _creatingPeriod;
    private bool _showCreatePeriodForm;
    private bool _isReminderDialogOpen;
    private bool _isSuspensionDialogOpen;
    private bool _loadingReminderDispatchPreview;
    private bool _loadingSuspensionPreview;
    private bool _sendingReminderDispatch;
    private bool _sendingSuspension;
    private readonly HashSet<int> _downloadingReminderPdfIds = [];
    private bool _downloadingSuspensionPdf;
    private bool _sendingReminderDispatchCompleted;
    private string? _errorMessage;
    private string? _statusMessage;
    private string? _reminderDialogError;
    private string? _suspensionDialogError;
    private string _reminderDialogTitle = "Send reminder emails";
    private MembershipDueDto? _suspensionDue;
    private MembershipDueEmailPreviewDto? _suspensionPreview;
    private MembershipDueReminderDispatchPreviewDto? _reminderDispatchPreview;
    private bool _canManageDues;
    private bool _canDispatchDues;

    protected override async Task OnInitializedAsync() {
        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authenticationState.User;
        _canManageDues = user.HasClaim("permission", "management.dues.manage");
        _canDispatchDues = user.HasClaim("permission", "management.dues.dispatch");
        await LoadMemberLookupAsync();
        await LoadPaymentPeriodsAsync();
    }

    private async Task LoadMemberLookupAsync() {
        var membersResult = await MemberApi.GetAllMembersAsync();
        if (!membersResult.IsSuccess)
            return;

        _memberFullNameLookup.Clear();
        foreach (var member in membersResult.Value ?? []) {
            var fullName = $"{member.FirstName} {member.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                continue;

            _memberFullNameLookup[member.Id] = fullName;
        }
    }

    private async Task LoadPaymentPeriodsAsync(bool preserveSelection = false) {
        _loadingPaymentPeriods = true;
        _errorMessage = null;

        var result = await MemberApi.GetPaymentPeriodsAsync();
        if (!result.IsSuccess) {
            _paymentPeriods.Clear();
            _dues.Clear();
            _selectedPaymentPeriodId = null;
            _errorMessage = result.Error ?? "Failed to load payment periods.";
            _loadingPaymentPeriods = false;
            return;
        }

        _paymentPeriods.Clear();
        _paymentPeriods.AddRange(result.Value ?? []);

        if (_paymentPeriods.Count == 0) {
            _dues.Clear();
            _selectedPaymentPeriodId = null;
            _statusMessage = "No payment periods exist yet. Create one to generate dues.";
            _loadingPaymentPeriods = false;
            return;
        }

        var selectedPeriodStillExists = _selectedPaymentPeriodId is not null
            && _paymentPeriods.Any(x => x.Id == _selectedPaymentPeriodId.Value);
        if (!preserveSelection || !selectedPeriodStillExists) {
            _selectedPaymentPeriodId = null;
            _dues.Clear();
        }

        _loadingPaymentPeriods = false;
        if (_selectedPaymentPeriodId is not null)
            await LoadSelectedPaymentPeriodDuesAsync();
    }

    private async Task LoadSelectedPaymentPeriodDuesAsync() {
        _loadingDues = true;
        _errorMessage = null;

        if (_selectedPaymentPeriodId is null) {
            _dues.Clear();
            _loadingDues = false;
            return;
        }

        var result = await MemberApi.GetPaymentPeriodDuesAsync(_selectedPaymentPeriodId.Value);
        if (!result.IsSuccess) {
            _dues.Clear();
            _errorMessage = result.Error ?? "Failed to load dues.";
            _loadingDues = false;
            return;
        }

        _dues.Clear();
        _dues.AddRange(result.Value ?? []);
        _loadingDues = false;
    }

    private async Task SelectPaymentPeriodAsync(MembershipPaymentPeriodDto paymentPeriod) {
        _selectedPaymentPeriodId = paymentPeriod.Id;
        _memberNameFilter = null;
        _selectedStatusFilter = null;
        _statusMessage = null;
        _errorMessage = null;
        await LoadSelectedPaymentPeriodDuesAsync();
    }

    private void ShowPaymentPeriodList() {
        _selectedPaymentPeriodId = null;
        _dues.Clear();
        _memberNameFilter = null;
        _selectedStatusFilter = null;
        _statusMessage = null;
        _errorMessage = null;
    }

    private async Task CreatePaymentPeriodAsync() {
        _creatingPeriod = true;
        _errorMessage = null;
        _statusMessage = null;

        var result = await MemberApi.CreatePaymentPeriodAsync(_createRequest);
        if (!result.IsSuccess) {
            _errorMessage = result.Error ?? "Failed to create payment period.";
            _creatingPeriod = false;
            return;
        }

        _statusMessage = $"Payment period '{result.Value!.Name}' created.";
        _showCreatePeriodForm = false;
        _creatingPeriod = false;
        await LoadPaymentPeriodsAsync();
    }

    private async Task SaveDueAsync(MembershipDueDto due) {
        _savingDueIds.Add(due.Id);
        _errorMessage = null;
        _statusMessage = null;

        var result = await MemberApi.UpdateDueAsync(due.Id, due);
        if (!result.IsSuccess) {
            _errorMessage = result.Error ?? $"Failed to update due {due.Id}.";
            _savingDueIds.Remove(due.Id);
            return;
        }

        var existingDue = _dues.FirstOrDefault(x => x.Id == due.Id);
        if (existingDue is not null) {
            existingDue.Status = due.Status;
            existingDue.DueAmount = due.DueAmount;
            existingDue.PaidAmount = due.PaidAmount;
            existingDue.DueDate = due.DueDate;
            existingDue.SettledAt = due.SettledAt;
            existingDue.SettlementReference = due.SettlementReference;
        }

        _statusMessage = $"Due {due.Id} updated.";
        _savingDueIds.Remove(due.Id);
    }

    private void ToggleCreatePeriodForm() {
        _showCreatePeriodForm = !_showCreatePeriodForm;
    }

    private async Task OpenBulkReminderDialogAsync() {
        if (_selectedPaymentPeriodId is null)
            return;

        _reminderDialogTitle = $"Send reminders for {GetSelectedPaymentPeriodLabel()}";
        await OpenReminderDialogAsync(() => MemberApi.GetReminderDispatchPreviewForPaymentPeriodAsync(_selectedPaymentPeriodId.Value));
    }

    private async Task OpenSingleReminderDialogAsync(MembershipDueDto due) {
        _reminderDialogTitle = $"Send reminder for {GetPrimaryMemberLabel(due.MemberId)}";
        await OpenReminderDialogAsync(() => MemberApi.GetReminderDispatchPreviewForDueAsync(due.Id));
    }

    private async Task OpenSuspensionDialogAsync(MembershipDueDto due) {
        _isSuspensionDialogOpen = true;
        _loadingSuspensionPreview = true;
        _sendingSuspension = false;
        _suspensionDialogError = null;
        _suspensionDue = due;
        _suspensionPreview = null;

        var result = await MemberApi.GetSuspensionEmailPreviewAsync(due.Id);
        if (!result.IsSuccess) {
            _suspensionDialogError = result.Error ?? "Failed to load suspension preview.";
            _loadingSuspensionPreview = false;
            return;
        }

        _suspensionPreview = result.Value;
        _loadingSuspensionPreview = false;
    }

    private async Task OpenReminderDialogAsync(Func<Task<Result<MembershipDueReminderDispatchPreviewDto>>> loader) {
        _isReminderDialogOpen = true;
        _loadingReminderDispatchPreview = true;
        _sendingReminderDispatch = false;
        _sendingReminderDispatchCompleted = false;
        _reminderDialogError = null;
        _reminderDispatchPreview = null;
        _dispatchRecipientStates = [];

        var result = await loader();
        if (!result.IsSuccess) {
            _reminderDialogError = result.Error ?? "Failed to load reminder dispatch preview.";
            _loadingReminderDispatchPreview = false;
            return;
        }

        _reminderDispatchPreview = result.Value;
        _dispatchRecipientStates = (_reminderDispatchPreview?.Recipients ?? [])
            .Select(recipient => new ReminderDispatchRecipientState {
                DueId = recipient.DueId,
                MemberId = recipient.MemberId,
                MemberDisplayName = recipient.MemberDisplayName,
                Email = recipient.Email,
                DueAmount = recipient.DueAmount,
                DueDate = recipient.DueDate
            })
            .ToList();
        _loadingReminderDispatchPreview = false;
    }

    private async Task SendReminderDispatchAsync() {
        if (_dispatchRecipientStates.Count == 0)
            return;

        _sendingReminderDispatch = true;
        _sendingReminderDispatchCompleted = false;
        _reminderDialogError = null;

        foreach (var recipient in _dispatchRecipientStates) {
            recipient.State = ReminderDispatchState.Sending;
            recipient.ResultMessage = null;
            await InvokeAsync(StateHasChanged);

            var result = await MemberApi.SendReminderEmailAsync(recipient.DueId);
            if (result.IsSuccess) {
                recipient.State = ReminderDispatchState.Succeeded;
                recipient.ResultMessage = "Reminder email sent.";
            }
            else {
                recipient.State = ReminderDispatchState.Failed;
                recipient.ResultMessage = result.Error ?? "Failed to send reminder email.";
            }

            await InvokeAsync(StateHasChanged);
        }

        _sendingReminderDispatch = false;
        _sendingReminderDispatchCompleted = true;
        await LoadSelectedPaymentPeriodDuesAsync();
        _statusMessage = $"Reminder dispatch finished: {ReminderDispatchSuccessCount} succeeded, {ReminderDispatchFailureCount} failed.";
    }

    private void CloseReminderDialog() {
        if (_sendingReminderDispatch)
            return;

        _isReminderDialogOpen = false;
        _loadingReminderDispatchPreview = false;
        _sendingReminderDispatch = false;
        _sendingReminderDispatchCompleted = false;
        _reminderDialogError = null;
        _reminderDispatchPreview = null;
        _dispatchRecipientStates = [];
    }

    private async Task SendSuspensionAsync() {
        if (_suspensionDue is null)
            return;

        _sendingSuspension = true;
        _suspensionDialogError = null;

        var result = await MemberApi.SendSuspensionEmailAsync(_suspensionDue.Id);
        if (!result.IsSuccess) {
            _suspensionDialogError = result.Error ?? "Failed to send suspension email.";
            _sendingSuspension = false;
            return;
        }

        _sendingSuspension = false;
        _statusMessage = $"Suspension email sent and {GetPrimaryMemberLabel(_suspensionDue.MemberId)} was suspended.";
        CloseSuspensionDialog();
        await LoadMemberLookupAsync();
        await LoadSelectedPaymentPeriodDuesAsync();
    }

    private void CloseSuspensionDialog() {
        if (_sendingSuspension)
            return;

        _isSuspensionDialogOpen = false;
        _loadingSuspensionPreview = false;
        _sendingSuspension = false;
        _suspensionDialogError = null;
        _suspensionDue = null;
        _suspensionPreview = null;
    }

    private async Task DownloadReminderPdfAsync(ReminderDispatchRecipientState recipient) {
        _downloadingReminderPdfIds.Add(recipient.DueId);
        recipient.ResultMessage = null;
        var result = await MemberApi.GetReminderPdfAsync(recipient.DueId);
        _downloadingReminderPdfIds.Remove(recipient.DueId);
        if (!result.IsSuccess) {
            recipient.ResultMessage = result.Error ?? "Failed to generate reminder PDF.";
            return;
        }

        await DownloadPdfAsync(result.Value!, $"membership-due-reminder-{recipient.DueId}.pdf");
    }

    private async Task DownloadSuspensionPdfAsync() {
        if (_suspensionDue is null)
            return;

        _downloadingSuspensionPdf = true;
        _suspensionDialogError = null;
        var result = await MemberApi.GetSuspensionPdfAsync(_suspensionDue.Id);
        _downloadingSuspensionPdf = false;
        if (!result.IsSuccess) {
            _suspensionDialogError = result.Error ?? "Failed to generate suspension PDF.";
            return;
        }

        await DownloadPdfAsync(result.Value!, $"membership-suspension-{_suspensionDue.Id}.pdf");
    }

    private async Task DownloadPdfAsync(byte[] pdf, string fileName) {
        var base64 = Convert.ToBase64String(pdf);
        await Js.InvokeVoidAsync("akGaming.downloadFile", fileName, "application/pdf", base64);
    }

    private int PendingCount => _dues.Count(x => x.Status == MembershipDueStatus.Pending);
    private int PaidCount => _dues.Count(x => x.Status == MembershipDueStatus.Paid);
    private int CancelledCount => _dues.Count(x => x.Status == MembershipDueStatus.Cancelled);
    private int WaivedCount => _dues.Count(x => x.Status == MembershipDueStatus.Waived);
    private int TotalCount => _dues.Count;
    private MembershipPaymentPeriodDto? SelectedPaymentPeriod =>
        _paymentPeriods.FirstOrDefault(period => period.Id == _selectedPaymentPeriodId);
    private IEnumerable<MembershipDueDto> FilteredAndSortedDues => BuildFilteredAndSortedDues();
    private string ReminderDialogTitle => _reminderDialogTitle;
    private string SuspensionDialogTitle => _suspensionDue is null
        ? "Send suspension email"
        : $"Send suspension for {GetPrimaryMemberLabel(_suspensionDue.MemberId)}";
    private MarkupString SuspensionHtmlPreview => new(_suspensionPreview?.HtmlBody ?? string.Empty);
    private int ReminderDispatchSuccessCount => _dispatchRecipientStates.Count(x => x.State == ReminderDispatchState.Succeeded);
    private int ReminderDispatchFailureCount => _dispatchRecipientStates.Count(x => x.State == ReminderDispatchState.Failed);

    private string GetHalfPieStyle() {
        if (TotalCount == 0)
            return "background: conic-gradient(from -90deg, var(--dues-empty) 0deg 180deg, transparent 180deg 360deg);";

        var paidEnd = ToHalfPieAngle(PaidCount);
        var pendingEnd = paidEnd + ToHalfPieAngle(PendingCount);
        var waivedEnd = pendingEnd + ToHalfPieAngle(WaivedCount);
        var cancelledEnd = waivedEnd + ToHalfPieAngle(CancelledCount);
        var fullEnd = Math.Min(cancelledEnd, 180m);

        return $"background: conic-gradient(from -90deg," +
               $"var(--dues-paid) 0deg {paidEnd}deg," +
               $"var(--dues-open) {paidEnd}deg {pendingEnd}deg," +
               $"var(--dues-waived) {pendingEnd}deg {waivedEnd}deg," +
               $"var(--dues-cancelled) {waivedEnd}deg {cancelledEnd}deg," +
               $"var(--dues-empty) {fullEnd}deg 180deg," +
               $"transparent 180deg 360deg);";
    }

    private decimal ToHalfPieAngle(int count) => 180m * count / TotalCount;

    private IEnumerable<MembershipDueDto> BuildFilteredAndSortedDues() {
        IEnumerable<MembershipDueDto> query = _dues;

        if (_selectedStatusFilter is not null)
            query = query.Where(x => x.Status == _selectedStatusFilter.Value);

        if (!string.IsNullOrWhiteSpace(_memberNameFilter)) {
            var term = _memberNameFilter.Trim();
            query = query.Where(x => GetMemberSearchText(x.MemberId).Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (_memberDisplayMode == MemberDisplayMode.FullName) {
            query = query
                .OrderBy(x => GetMemberSortName(x.MemberId), StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.DueDate)
                .ThenBy(x => x.MemberId);
        }
        else {
            query = query
                .OrderBy(x => x.DueDate)
                .ThenBy(x => x.MemberId);
        }

        return query;
    }

    private string GetMemberSortName(Guid memberId) {
        if (_memberFullNameLookup.TryGetValue(memberId, out var fullName) && !string.IsNullOrWhiteSpace(fullName))
            return fullName;

        return memberId.ToString();
    }

    private string GetMemberSearchText(Guid memberId) {
        if (_memberFullNameLookup.TryGetValue(memberId, out var fullName) && !string.IsNullOrWhiteSpace(fullName))
            return fullName;

        return memberId.ToString();
    }

    private string GetPrimaryMemberLabel(Guid memberId) {
        if (_memberDisplayMode == MemberDisplayMode.MemberId)
            return memberId.ToString();

        if (_memberFullNameLookup.TryGetValue(memberId, out var fullName))
            return fullName;

        return memberId.ToString();
    }

    private string? GetSecondaryMemberLabel(Guid memberId) {
        if (_memberDisplayMode == MemberDisplayMode.FullName && _memberFullNameLookup.ContainsKey(memberId))
            return memberId.ToString();

        return null;
    }

    private string GetSelectedPaymentPeriodLabel() =>
        _paymentPeriods.FirstOrDefault(period => period.Id == _selectedPaymentPeriodId)?.Name ?? "selected payment period";

    private static string GetReminderStateLabel(ReminderDispatchState state) => state switch {
        ReminderDispatchState.Pending => "Ready",
        ReminderDispatchState.Sending => "Sending",
        ReminderDispatchState.Succeeded => "Sent",
        ReminderDispatchState.Failed => "Failed",
        _ => "Ready"
    };

    private static string GetReminderStateClass(ReminderDispatchState state) => state switch {
        ReminderDispatchState.Pending => "dues-reminder-state dues-reminder-state-pending",
        ReminderDispatchState.Sending => "dues-reminder-state dues-reminder-state-sending",
        ReminderDispatchState.Succeeded => "dues-reminder-state dues-reminder-state-succeeded",
        ReminderDispatchState.Failed => "dues-reminder-state dues-reminder-state-failed",
        _ => "dues-reminder-state dues-reminder-state-pending"
    };

    private enum MemberDisplayMode {
        FullName,
        MemberId
    }

    private enum ReminderDispatchState {
        Pending,
        Sending,
        Succeeded,
        Failed
    }

    private sealed class ReminderDispatchRecipientState {
        public int DueId { get; set; }
        public Guid MemberId { get; set; }
        public string MemberDisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal DueAmount { get; set; }
        public DateOnly DueDate { get; set; }
        public ReminderDispatchState State { get; set; } = ReminderDispatchState.Pending;
        public string? ResultMessage { get; set; }
    }
}
