using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Frontend.Localization;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement;

public partial class MemberManagementPage : ComponentBase {
    
    [Inject] 
    private MemberManagementApiClient MemberApi { get; set; } = default!;
    
    private List<MemberDto>? _members;

    private string? _createError;

    private MemberDto? _selectedMember = null;

    private bool _canManageMemberDetails;
    private bool _canManageMemberStatus;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    protected override async Task OnInitializedAsync() {
        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authenticationState.User;
        _canManageMemberDetails = user.HasClaim("permission", "management.members.details.manage");
        _canManageMemberStatus = user.HasClaim("permission", "management.members.status.manage");
        await LoadMembersAsync();
    }

    private async Task LoadMembersAsync() {
        try {
            var result = await MemberApi.GetAllMembersAsync();
            if (result.IsSuccess) {
                _members = result.Value?.ToList();
            }
                
        }
        catch (Exception ex) {
            Console.WriteLine("Error fetching members: " + ex);
            _members = new();
        }
        StateHasChanged();
    }
    
    private void SelectMember(MemberDto member) {
        _selectedMember = member;

        StateHasChanged();
    }
    
    private void SelectMember(Guid id) {
        _selectedMember = _members?.FirstOrDefault(m => m.Id == id);

        StateHasChanged();
    }
    
    private async Task Reload(MemberDto member) {
        await LoadMembersAsync();
        StateHasChanged();
        SelectMember(member.Id);
    }
    
    private async Task CreateMember() {
        var creationResult = await MemberApi.CreateMemberAsync(new MemberCreationDto() { Address = new AddressDto() });
        if (!creationResult.IsSuccess) {
            _createError = creationResult.Error;
            return;
        }
        var newMemberId = creationResult.Value;
        await LoadMembersAsync();
        SelectMember(newMemberId);
    }
    
    private enum MemberColumn { Name, Email, Discord, Status }
    private static readonly MembershipStatus[] AllStatuses = Enum.GetValues<MembershipStatus>();
    private HashSet<MembershipStatus> _statuses = DefaultStatuses();
    private string _nameFilter = string.Empty;
    private string _emailFilter = string.Empty;
    private string _discordFilter = string.Empty;
    private MemberColumn _sortColumn = MemberColumn.Name;
    private bool _sortDescending;

    private static HashSet<MembershipStatus> DefaultStatuses()
    {
        return AllStatuses.Except(new[] { MembershipStatus.None, MembershipStatus.Suspended,
            MembershipStatus.Expelled, MembershipStatus.Withdrawn, MembershipStatus.ApplicationRejected,
            MembershipStatus.Applicant }).ToHashSet();
    }

    private static string DisplayName(MemberDto member)
    {
        return $"{member.LastName}, {member.FirstName}".Trim(' ', ',');
    }

    private static bool Matches(string? value, string filter)
    {
        return string.IsNullOrWhiteSpace(filter) || (value?.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private List<MemberDto> FilteredMembers
    {
        get
        {
            var members = (_members ?? []).Where(member => _statuses.Contains(member.Status)
                && (Matches($"{member.FirstName} {member.LastName}", _nameFilter) || Matches(DisplayName(member), _nameFilter))
                && Matches(member.Email, _emailFilter) && Matches(member.DiscordUserName, _discordFilter));
            Func<MemberDto, string?> key = _sortColumn switch
            {
                MemberColumn.Email => member => member.Email,
                MemberColumn.Discord => member => member.DiscordUserName,
                MemberColumn.Status => member => member.Status.ToLocalizedString(SharedText),
                _ => DisplayName
            };
            var sorted = _sortDescending
                ? members.OrderByDescending(key, StringComparer.CurrentCultureIgnoreCase)
                : members.OrderBy(key, StringComparer.CurrentCultureIgnoreCase);
            return sorted.ThenBy(DisplayName, StringComparer.CurrentCultureIgnoreCase).ThenBy(member => member.Id).ToList();
        }
    }

    private string ColumnLabel(MemberColumn column)
    {
        return SharedText[column switch
        {
            MemberColumn.Email => "Field_Email",
            MemberColumn.Discord => "Field_Discord",
            MemberColumn.Status => "Members_Status",
            _ => "Members_Name"
        }];
    }

    private void SortBy(MemberColumn column)
    {
        _sortDescending = _sortColumn == column && !_sortDescending;
        _sortColumn = column;
    }

    private string SortAria(MemberColumn column)
    {
        return column != _sortColumn ? "none" : _sortDescending ? "descending" : "ascending";
    }

    private string SortIndicator(MemberColumn column)
    {
        return column != _sortColumn ? "↕" : _sortDescending ? "↓" : "↑";
    }

    private void ToggleStatus(MembershipStatus status)
    {
        if (!_statuses.Remove(status))
        {
            _statuses.Add(status);
        }
    }

    private void ResetFilters()
    {
        _nameFilter = _emailFilter = _discordFilter = string.Empty;
        _statuses = DefaultStatuses();
    }

    private void CloseDetails()
    {
        _selectedMember = null;
    }
}
