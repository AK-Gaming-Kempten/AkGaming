using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement;

public partial class MemberManagementDuesPage : ComponentBase {
    [Inject] private MemberManagementApiClient MemberApi { get; set; } = default!;

    private MembershipPaymentPeriodCreateDto _createRequest = new() {
        Name = $"{DateTime.UtcNow:yyyy-MM}",
        DueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
        DefaultDueAmount = 10m
    };

    private readonly List<MembershipPaymentPeriodDto> _paymentPeriods = [];
    private readonly List<MembershipDueDto> _dues = [];
    private readonly Dictionary<Guid, string> _memberFullNameLookup = [];
    private readonly HashSet<int> _savingDueIds = [];
    private int? _selectedPaymentPeriodId;
    private MemberDisplayMode _memberDisplayMode = MemberDisplayMode.FullName;
    private string? _memberNameFilter;
    private MembershipDueStatus? _selectedStatusFilter;

    private bool _loadingPaymentPeriods;
    private bool _loadingDues;
    private bool _creatingPeriod;
    private bool _showCreatePeriodForm;
    private string? _errorMessage;
    private string? _statusMessage;

    protected override async Task OnInitializedAsync() {
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

        if (!preserveSelection || _selectedPaymentPeriodId is null || _paymentPeriods.All(x => x.Id != _selectedPaymentPeriodId.Value))
            _selectedPaymentPeriodId = _paymentPeriods[0].Id;

        _loadingPaymentPeriods = false;
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

    private async Task OnPaymentPeriodChanged(ChangeEventArgs args) {
        var raw = args.Value?.ToString();
        if (!int.TryParse(raw, out var paymentPeriodId))
            return;

        _selectedPaymentPeriodId = paymentPeriodId;
        await LoadSelectedPaymentPeriodDuesAsync();
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

    private int PendingCount => _dues.Count(x => x.Status == MembershipDueStatus.Pending);
    private int PaidCount => _dues.Count(x => x.Status == MembershipDueStatus.Paid);
    private int CancelledCount => _dues.Count(x => x.Status == MembershipDueStatus.Cancelled);
    private int WaivedCount => _dues.Count(x => x.Status == MembershipDueStatus.Waived);
    private int TotalCount => _dues.Count;
    private IEnumerable<MembershipDueDto> FilteredAndSortedDues => BuildFilteredAndSortedDues();

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

    private enum MemberDisplayMode {
        FullName,
        MemberId
    }
}
