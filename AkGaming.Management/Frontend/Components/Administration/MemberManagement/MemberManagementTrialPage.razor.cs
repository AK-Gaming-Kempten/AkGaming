using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement;

public partial class MemberManagementTrialPage : ComponentBase {
    [Inject]
    private MemberManagementApiClient MemberApi { get; set; } = default!;

    private readonly List<TrialMemberDto> _trialMembers = [];

    private MemberDto? _selectedMember;
    private bool _isLoading = true;
    private string? _loadError;
    private bool _isMobileDetailOpen;
    private bool _canManageMemberDetails;
    private bool _canManageMemberStatus;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    protected override async Task OnInitializedAsync() {
        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authenticationState.User;
        _canManageMemberDetails = user.HasClaim("permission", "management.members.details.manage");
        _canManageMemberStatus = user.HasClaim("permission", "management.members.status.manage");
        await LoadTrialMembersAsync();
    }

    private async Task LoadTrialMembersAsync() {
        _isLoading = true;
        _loadError = null;
        _trialMembers.Clear();

        var result = await MemberApi.GetTrialMembersAsync();
        if (!result.IsSuccess) {
            _loadError = result.Error;
            _isLoading = false;
            return;
        }

        _trialMembers.AddRange(result.Value ?? []);

        var previousSelectedMemberId = _selectedMember?.Id;
        _selectedMember = previousSelectedMemberId.HasValue
            ? _trialMembers.FirstOrDefault(item => item.Member.Id == previousSelectedMemberId.Value)?.Member
            : _trialMembers.FirstOrDefault()?.Member;

        _isLoading = false;
    }

    private static TrialPeriodInfo GetTrialInfo(TrialMemberDto trialMember) {
        if (!trialMember.TrialEndsAt.HasValue)
            return TrialPeriodInfo.Error();

        var nowDate = DateTime.UtcNow.Date;
        var endDate = trialMember.TrialEndsAt.Value.Date;
        var daysRemaining = (endDate - nowDate).Days;
        return new TrialPeriodInfo(
            EndDate: endDate,
            DaysRemaining: Math.Max(daysRemaining, 0),
            IsExpired: daysRemaining < 0,
            HasError: false
        );
    }

    private void SelectMember(MemberDto member) {
        _selectedMember = member;
        _isMobileDetailOpen = true;
    }

    private async Task ReloadAfterUpdate(MemberDto member) {
        await LoadTrialMembersAsync();
        _selectedMember = _trialMembers.FirstOrDefault(item => item.Member.Id == member.Id)?.Member;
        _isMobileDetailOpen = _selectedMember is not null;
    }

    private void ShowListMobile() {
        _isMobileDetailOpen = false;
        _selectedMember = null;
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
