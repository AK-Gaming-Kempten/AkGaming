using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Teams.Components;

public partial class TeamDetailsView : ComponentBase
{
    [Parameter] public TeamDto Team { get; set; } = default!;
    [Parameter] public string GameName { get; set; } = string.Empty;
    [Parameter] public IReadOnlyList<PlayerProfileDto> AvailableProfiles { get; set; } = [];
    [Parameter] public IReadOnlyList<TournamentRegistrationDto> Registrations { get; set; } = [];
    [Parameter] public IReadOnlyDictionary<Guid, string> TournamentNames { get; set; } = new Dictionary<Guid, string>();
    [Parameter] public bool IsAuthenticated { get; set; }
    [Parameter] public string? CurrentUserId { get; set; }
    [Parameter] public string? CurrentUserDisplayName { get; set; }
    [Parameter] public string TeamName { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> TeamNameChanged { get; set; }
    [Parameter] public string TeamPrimaryColor { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> TeamPrimaryColorChanged { get; set; }
    [Parameter] public bool IsTeamEditMode { get; set; }
    [Parameter] public string GuestName { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> GuestNameChanged { get; set; }
    [Parameter] public int? GuestRankRating { get; set; }
    [Parameter] public EventCallback<int?> GuestRankRatingChanged { get; set; }
    [Parameter] public bool IsGuestFormVisible { get; set; }
    [Parameter] public PlayerProfileDto? EditingGuestProfile { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<MediaAssetDto> OnLogoUploaded { get; set; }
    [Parameter] public EventCallback OnLogoCleared { get; set; }
    [Parameter] public EventCallback<MediaAssetDto> OnBannerUploaded { get; set; }
    [Parameter] public EventCallback OnBannerCleared { get; set; }
    [Parameter] public EventCallback OnTeamEditRequested { get; set; }
    [Parameter] public EventCallback OnTeamSubmitted { get; set; }
    [Parameter] public EventCallback OnTeamEditCanceled { get; set; }
    [Parameter] public EventCallback OnInviteManagementRequested { get; set; }
    [Parameter] public EventCallback OnGuestCreateRequested { get; set; }
    [Parameter] public EventCallback OnGuestSubmitted { get; set; }
    [Parameter] public EventCallback<PlayerProfileDto> OnGuestEditRequested { get; set; }
    [Parameter] public EventCallback OnGuestEditCanceled { get; set; }
    [Parameter] public EventCallback<PlayerProfileDto> OnGuestDeleteRequested { get; set; }
    [Parameter] public EventCallback<TeamMembershipDto> OnPromoteToEditorRequested { get; set; }
    [Parameter] public EventCallback<TeamMembershipDto> OnDemoteToMemberRequested { get; set; }
    [Parameter] public EventCallback<TeamMembershipDto> OnTransferOwnershipRequested { get; set; }

    private bool CanEditTeam => !string.IsNullOrWhiteSpace(CurrentUserId)
                                && Team.Memberships.Any(member =>
                                    string.Equals(member.UserId, CurrentUserId, StringComparison.OrdinalIgnoreCase)
                                    && (member.Role == TeamRoleDto.Owner || member.Role == TeamRoleDto.Editor));

    private bool CanManageRoles => !string.IsNullOrWhiteSpace(CurrentUserId)
                                   && Team.Memberships.Any(member =>
                                       string.Equals(member.UserId, CurrentUserId, StringComparison.OrdinalIgnoreCase)
                                       && member.Role == TeamRoleDto.Owner);

    private string GuestSubmitLabel => EditingGuestProfile is null ? "Add guest" : "Save guest";

    private Task HandleGuestNameChanged(ChangeEventArgs args)
        => GuestNameChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);

    private Task HandleTeamNameChanged(ChangeEventArgs args)
        => TeamNameChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);

    private Task HandleTeamPrimaryColorChanged(ChangeEventArgs args)
        => TeamPrimaryColorChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);

    private string GetMemberInitial(TeamMembershipDto member)
    {
        var displayName = GetMemberDisplayName(member);
        return string.IsNullOrWhiteSpace(displayName) ? "?" : displayName[..1].ToUpperInvariant();
    }

    private string GetMemberDisplayName(TeamMembershipDto member)
    {
        if (!string.IsNullOrWhiteSpace(CurrentUserId)
            && string.Equals(member.UserId, CurrentUserId, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(CurrentUserDisplayName))
        {
            return CurrentUserDisplayName;
        }

        var profileName = AvailableProfiles
            .FirstOrDefault(profile => profile.Type == PlayerProfileTypeDto.User
                                       && !string.IsNullOrWhiteSpace(profile.UserId)
                                       && string.Equals(profile.UserId, member.UserId, StringComparison.OrdinalIgnoreCase))
            ?.Name;

        return !string.IsNullOrWhiteSpace(profileName) ? profileName : member.UserId;
    }
}
