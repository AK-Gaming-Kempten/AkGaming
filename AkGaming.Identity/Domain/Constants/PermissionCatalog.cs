namespace AkGaming.Identity.Domain.Constants;

public sealed record PermissionDefinition(string Key, string Application, string Area, string Operation, string Description);

public static class PermissionCatalog
{
    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(PermissionNames.IdentityUsersRead, "identity", "users", "read", "View identity users."),
        new(PermissionNames.IdentityUsersManage, "identity", "users", "manage", "Manage identity users and their role assignments."),
        new(PermissionNames.IdentityRolesRead, "identity", "roles", "read", "View roles and their permissions."),
        new(PermissionNames.IdentityRolesManage, "identity", "roles", "manage", "Create, update, delete, and configure roles."),
        new(PermissionNames.IdentityAuditRead, "identity", "audit", "read", "View identity audit logs."),
        new(PermissionNames.IdentityOidcManage, "identity", "oidc", "manage", "Manage OpenID Connect clients and scopes."),
        new(PermissionNames.ManagementMembersRead, "management", "members", "read", "View member records."),
        new(PermissionNames.ManagementMembersManage, "management", "members", "manage", "Create, change, and delete member records."),
        new(PermissionNames.ManagementMemberDetailsManage, "management", "member-details", "manage", "Edit member details and manage member links."),
        new(PermissionNames.ManagementMemberStatusManage, "management", "member-status", "manage", "Change membership status and status history."),
        new(PermissionNames.ManagementMembershipsRead, "management", "memberships", "read", "View membership applications and dues."),
        new(PermissionNames.ManagementMembershipsManage, "management", "memberships", "manage", "Manage membership applications and dues."),
        new(PermissionNames.ManagementDuesRead, "management", "dues", "read", "View membership dues and payment periods."),
        new(PermissionNames.ManagementDuesManage, "management", "dues", "manage", "Create payment periods and edit membership dues."),
        new(PermissionNames.ManagementDuesDispatch, "management", "dues", "dispatch", "Send membership reminders and suspensions."),
        new(PermissionNames.ManagementRequestsRead, "management", "requests", "read", "View membership and member-linking requests."),
        new(PermissionNames.ManagementRequestsManage, "management", "requests", "manage", "Accept, reject, and resolve membership and member-linking requests."),
        new(PermissionNames.ManagementInvoicesManage, "management", "invoices", "manage", "Manage invoices and invoice presets."),
        new(PermissionNames.ManagementDisbursementsRead, "management", "disbursements", "read", "View reimbursement cases, payout events, allocations, and applications."),
        new(PermissionNames.ManagementDisbursementsManage, "management", "disbursements", "manage", "Manage reimbursement statuses, payout events, allocations, and application statuses."),
        new(PermissionNames.ManagementGeneralMeetingsManage, "management", "general-meetings", "manage", "Create and administer general meetings, attendance, agendas, and ballots."),
        new(PermissionNames.ManagementGeneralMeetingsMinutesWrite, "management", "general-meetings", "minutes.write", "Write minutes for general meetings."),
        new(PermissionNames.ManagementGeneralMeetingsAuditRead, "management", "general-meetings", "audit.read", "View general-meeting audit records."),
        new(PermissionNames.TournamentsGamesManage, "tournaments", "games", "manage", "Manage tournament games."),
        new(PermissionNames.TournamentsTournamentsManage, "tournaments", "tournaments", "manage", "Manage tournaments."),
        new(PermissionNames.TournamentsRegistrationsManage, "tournaments", "registrations", "manage", "Manage tournament registrations and check-ins."),
        new(PermissionNames.TournamentsTeamsManage, "tournaments", "teams", "manage", "Manage teams on behalf of users."),
        new(PermissionNames.TournamentsPlayerProfilesManage, "tournaments", "player-profiles", "manage", "Manage player profiles on behalf of users."),
        new(PermissionNames.WebsiteCmsPostsManage, "website", "cms-posts", "manage", "Create and edit website posts, events, and folders."),
        new(PermissionNames.WebsiteCmsPostsPublish, "website", "cms-posts", "publish", "Publish website posts and events."),
        new(PermissionNames.WebsiteCmsMediaManage, "website", "cms-media", "manage", "Manage website media files and folders."),
        new(PermissionNames.WebsiteCmsHighlightsManage, "website", "cms-highlights", "manage", "Manage website homepage highlights."),
        new(PermissionNames.WebsiteCmsEsportsManage, "website", "cms-esports", "manage", "Manage website esports teams, games, and leagues.")
    ];
}
