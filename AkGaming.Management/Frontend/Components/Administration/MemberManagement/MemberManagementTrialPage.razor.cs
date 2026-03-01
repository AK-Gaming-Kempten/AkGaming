using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement;

public partial class MemberManagementTrialPage : ComponentBase {
    [Inject]
    private MemberManagementApiClient MemberApi { get; set; } = default!;

    private readonly List<MemberDto> _trialMembers = [];
    private readonly Dictionary<Guid, TrialPeriodInfo> _trialPeriodsByMemberId = [];

    private MemberDto? _selectedMember;
    private bool _isLoading = true;
    private string? _loadError;

    protected override async Task OnInitializedAsync() {
        await LoadTrialMembersAsync();
    }

    private async Task LoadTrialMembersAsync() {
        _isLoading = true;
        _loadError = null;
        _trialMembers.Clear();
        _trialPeriodsByMemberId.Clear();

        var result = await MemberApi.GetMembersWithStatusAsync(MembershipStatus.InTrial);
        if (!result.IsSuccess) {
            _loadError = result.Error;
            _isLoading = false;
            return;
        }

        _trialMembers.AddRange(result.Value ?? []);

        var previousSelectedMemberId = _selectedMember?.Id;
        _selectedMember = previousSelectedMemberId.HasValue
            ? _trialMembers.FirstOrDefault(m => m.Id == previousSelectedMemberId.Value)
            : _trialMembers.FirstOrDefault();

        await LoadTrialPeriodsAsync(_trialMembers);

        // Show only members whose default trial period is still active.
        _trialMembers.RemoveAll(member => {
            var trialInfo = _trialPeriodsByMemberId.GetValueOrDefault(member.Id);
            return trialInfo is null || trialInfo.HasError || trialInfo.IsExpired;
        });

        if (_selectedMember is not null && _trialMembers.All(m => m.Id != _selectedMember.Id)) {
            _selectedMember = _trialMembers.FirstOrDefault();
        }

        _isLoading = false;
    }

    private async Task LoadTrialPeriodsAsync(IEnumerable<MemberDto> members) {
        var nowDate = DateTime.UtcNow.Date;
        var tasks = members.Select(async member => {
            var endResult = await MemberApi.GetDefaultEndOfTrialPeriodAsync(member.Id);
            if (!endResult.IsSuccess) {
                _trialPeriodsByMemberId[member.Id] = TrialPeriodInfo.Error();
                return;
            }

            var endDate = endResult.Value.Date;
            var daysRemaining = (endDate - nowDate).Days;
            _trialPeriodsByMemberId[member.Id] = new TrialPeriodInfo(
                EndDate: endDate,
                DaysRemaining: Math.Max(daysRemaining, 0),
                IsExpired: daysRemaining < 0,
                HasError: false
            );
        });

        await Task.WhenAll(tasks);
    }

    private TrialPeriodInfo? GetTrialInfo(Guid memberId) {
        return _trialPeriodsByMemberId.GetValueOrDefault(memberId);
    }

    private void SelectMember(MemberDto member) {
        _selectedMember = member;
    }

    private async Task ReloadAfterUpdate(MemberDto member) {
        await LoadTrialMembersAsync();
        _selectedMember = _trialMembers.FirstOrDefault(m => m.Id == member.Id);
    }

    private sealed record TrialPeriodInfo(
        DateTime EndDate,
        int DaysRemaining,
        bool IsExpired,
        bool HasError
    ) {
        public static TrialPeriodInfo Error() => new(
            EndDate: DateTime.MinValue,
            DaysRemaining: 0,
            IsExpired: false,
            HasError: true
        );
    }
}
