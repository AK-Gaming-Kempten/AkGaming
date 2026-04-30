using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Administration;

public partial class TournamentRegistrationAdminList : ComponentBase
{
    [Parameter] public IReadOnlyList<TournamentRegistrationDto> Registrations { get; set; } = [];
    [Parameter] public IReadOnlyDictionary<Guid, string> TeamNames { get; set; } = new Dictionary<Guid, string>();
    [Parameter] public IReadOnlyDictionary<Guid, Guid?> TeamLogos { get; set; } = new Dictionary<Guid, Guid?>();
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback<TournamentRegistrationReviewAction> ReviewRequested { get; set; }
    [Parameter] public EventCallback<TournamentRosterReviewAction> RosterReviewRequested { get; set; }
    [Parameter] public EventCallback<Guid> DeleteRequested { get; set; }
    private Guid? pendingDeleteRegistrationId;
    private readonly HashSet<string> expandedRosterSections = [];

    private Task ReviewAsync(Guid registrationId, bool approve)
        => ReviewRequested.InvokeAsync(new TournamentRegistrationReviewAction(registrationId, approve));

    private Task RequestDeleteAsync(Guid registrationId)
    {
        pendingDeleteRegistrationId = registrationId;
        return Task.CompletedTask;
    }

    private Task CancelDeleteAsync()
    {
        pendingDeleteRegistrationId = null;
        return Task.CompletedTask;
    }

    private async Task ConfirmDeleteAsync()
    {
        if (pendingDeleteRegistrationId is null)
        {
            return;
        }

        var registrationId = pendingDeleteRegistrationId.Value;
        pendingDeleteRegistrationId = null;
        await DeleteRequested.InvokeAsync(registrationId);
    }

    private static RosterDto? GetActiveRoster(TournamentRegistrationDto registration)
    {
        if (registration.ActiveRosterId is null)
        {
            return null;
        }

        return registration.Rosters.FirstOrDefault(roster => roster.Id == registration.ActiveRosterId.Value);
    }

    private static RosterDto? GetPendingRoster(TournamentRegistrationDto registration)
        => registration.Rosters.FirstOrDefault(roster => roster.Status == RosterStatusDto.Pending);

    private bool IsDeleteDialogOpen(Guid registrationId)
        => pendingDeleteRegistrationId == registrationId;

    private Task ReviewRosterAsync(Guid registrationId, Guid rosterId, bool approve)
        => RosterReviewRequested.InvokeAsync(new TournamentRosterReviewAction(registrationId, rosterId, approve));

    private static string GetRosterSectionKey(Guid registrationId, Guid rosterId)
        => $"{registrationId:N}:{rosterId:N}";

    private bool IsRosterExpanded(Guid registrationId, Guid rosterId)
        => expandedRosterSections.Contains(GetRosterSectionKey(registrationId, rosterId));

    private Task ToggleRosterExpandedAsync(Guid registrationId, Guid rosterId)
    {
        var key = GetRosterSectionKey(registrationId, rosterId);
        if (!expandedRosterSections.Add(key))
        {
            expandedRosterSections.Remove(key);
        }

        return Task.CompletedTask;
    }

    private string GetTeamName(Guid teamId)
        => TeamNames.TryGetValue(teamId, out var name) ? name : teamId.ToString();

    private Guid? GetTeamLogoAssetId(Guid teamId)
        => TeamLogos.TryGetValue(teamId, out var logoAssetId) ? logoAssetId : null;

    private static string GetPlayerKind(RosterPlayerSnapshotDto snapshot)
        => snapshot.PlayerProfileType == PlayerProfileTypeDto.Guest ? "Guest profile" : "Member profile";

    private static string GetPlayerIdentity(RosterPlayerSnapshotDto snapshot)
    {
        if (snapshot.PlayerProfileType == PlayerProfileTypeDto.User)
        {
            return string.IsNullOrWhiteSpace(snapshot.UserId) ? "No user id" : $"User: {snapshot.UserId}";
        }

        return snapshot.SourcePlayerProfileId is Guid sourceProfileId
            ? $"Profile: {sourceProfileId}"
            : "Legacy snapshot";
    }

    private static bool HasUserLink(RosterPlayerSnapshotDto snapshot)
        => snapshot.PlayerProfileType == PlayerProfileTypeDto.User && !string.IsNullOrWhiteSpace(snapshot.UserId);

    private static string GetUserProfileHref(RosterPlayerSnapshotDto snapshot)
        => $"/discover?user={Uri.EscapeDataString(snapshot.UserId ?? string.Empty)}";

    private static string GetStatusClass(TournamentRegistrationStatusDto status)
        => status switch
        {
            TournamentRegistrationStatusDto.Approved => "status-pill-positive",
            TournamentRegistrationStatusDto.Pending => "status-pill-warn",
            _ => "status-pill-neutral"
        };
}

public sealed record TournamentRegistrationReviewAction(Guid RegistrationId, bool Approve);
public sealed record TournamentRosterReviewAction(Guid RegistrationId, Guid RosterId, bool Approve);
