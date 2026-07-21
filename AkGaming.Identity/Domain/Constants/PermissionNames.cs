namespace AkGaming.Identity.Domain.Constants;

public static class PermissionNames
{
    public const string ClaimType = "permission";

    public const string IdentityUsersRead = "identity.users.read";
    public const string IdentityUsersManage = "identity.users.manage";
    public const string IdentityRolesRead = "identity.roles.read";
    public const string IdentityRolesManage = "identity.roles.manage";
    public const string IdentityAuditRead = "identity.audit.read";
    public const string IdentityOidcManage = "identity.oidc.manage";

    public const string ManagementMembersRead = "management.members.read";
    public const string ManagementMembersManage = "management.members.manage";
    public const string ManagementMemberDetailsManage = "management.members.details.manage";
    public const string ManagementMemberStatusManage = "management.members.status.manage";
    public const string ManagementMembershipsRead = "management.memberships.read";
    public const string ManagementMembershipsManage = "management.memberships.manage";
    public const string ManagementDuesRead = "management.dues.read";
    public const string ManagementDuesManage = "management.dues.manage";
    public const string ManagementDuesDispatch = "management.dues.dispatch";
    public const string ManagementRequestsRead = "management.requests.read";
    public const string ManagementRequestsManage = "management.requests.manage";
    public const string ManagementInvoicesManage = "management.invoices.manage";
    public const string ManagementDisbursementsRead = "management.disbursements.read";
    public const string ManagementDisbursementsManage = "management.disbursements.manage";
    public const string ManagementGeneralMeetingsManage = "management.general-meetings.manage";
    public const string ManagementGeneralMeetingsMinutesWrite = "management.general-meetings.minutes.write";
    public const string ManagementGeneralMeetingsAuditRead = "management.general-meetings.audit.read";
    public const string ManagementBoardMeetingsRead = "management.board-meetings.read";
    public const string ManagementBoardMeetingsManage = "management.board-meetings.manage";

    public const string TournamentsGamesManage = "tournaments.games.manage";
    public const string TournamentsTournamentsManage = "tournaments.tournaments.manage";
    public const string TournamentsRegistrationsManage = "tournaments.registrations.manage";
    public const string TournamentsTeamsManage = "tournaments.teams.manage";
    public const string TournamentsPlayerProfilesManage = "tournaments.player-profiles.manage";

    public const string WebsiteCmsPostsManage = "website.cms.posts.manage";
    public const string WebsiteCmsPostsPublish = "website.cms.posts.publish";
    public const string WebsiteCmsMediaManage = "website.cms.media.manage";
    public const string WebsiteCmsHighlightsManage = "website.cms.highlights.manage";
    public const string WebsiteCmsEsportsManage = "website.cms.esports.manage";
}
