using AkGaming.Management.Frontend.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Management.Frontend.Components.Layout;

public partial class NavMenu : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] private IConfiguration Configuration { get; set; } = null!;
    [Inject] private GeneralMeetingAccessService GeneralMeetingAccess { get; set; } = null!;
    [Parameter] public EventCallback OnNavigate { get; set; }

    private bool isAuthenticated;
    private bool canReadMembers;
    private bool canManageMemberDetails;
    private bool canManageMemberStatus;
    private bool canReadDues;
    private bool canManageDues;
    private bool canDispatchDues;
    private bool canReadRequests;
    private bool canManageRequests;
    private bool canManageInvoices;
    private bool canReadDisbursements;
    private bool canReadUsers;
    private bool canAccessGeneralMeetings;
    private bool canReadBoardMeetings;
    private bool canReadRoles;
    private bool canReadIdentityAudit;
    private bool canManageOidc;
    private bool canAccessMemberManagement;
    private bool canAccessDues;
    private bool canAccessRequests;
    private bool canAccessIdentity;
    private bool isDebug;
    private bool isMemberManagementExpanded;
    private bool isIdentityExpanded;
    private bool isInvoicesExpanded;
    private bool isDisbursementsExpanded;
    private bool isDebugExpanded;

    private string? IdentityUrl
    {
        get
        {
            var authority = Configuration["OpenIdConnect:Authority"];
            return Uri.TryCreate(authority, UriKind.Absolute, out var uri)
                ? $"{uri.GetLeftPart(UriPartial.Authority)}{uri.AbsolutePath.TrimEnd('/')}/account/manage"
                : null;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        isAuthenticated = user.Identity?.IsAuthenticated ?? false;
        canReadMembers = isAuthenticated && user.HasClaim("permission", "management.members.read");
        canManageMemberDetails = isAuthenticated && user.HasClaim("permission", "management.members.details.manage");
        canManageMemberStatus = isAuthenticated && user.HasClaim("permission", "management.members.status.manage");
        canReadDues = isAuthenticated && user.HasClaim("permission", "management.dues.read");
        canManageDues = isAuthenticated && user.HasClaim("permission", "management.dues.manage");
        canDispatchDues = isAuthenticated && user.HasClaim("permission", "management.dues.dispatch");
        canReadRequests = isAuthenticated && user.HasClaim("permission", "management.requests.read");
        canManageRequests = isAuthenticated && user.HasClaim("permission", "management.requests.manage");
        canAccessDues = canReadDues;
        canAccessRequests = canReadRequests;
        canAccessMemberManagement = canReadMembers || canManageMemberDetails || canManageMemberStatus || canAccessDues || canAccessRequests;
        canManageInvoices = isAuthenticated && user.HasClaim("permission", "management.invoices.manage");
        canReadDisbursements = isAuthenticated && user.HasClaim("permission", "management.disbursements.read");
        canAccessGeneralMeetings = isAuthenticated && await GeneralMeetingAccess.CanAccessAsync(user);
        canReadBoardMeetings = isAuthenticated && user.HasClaim("permission", "management.board-meetings.read");
        canReadUsers = isAuthenticated && user.HasClaim("permission", "identity.users.read");
        canReadRoles = isAuthenticated && user.HasClaim("permission", "identity.roles.read");
        canReadIdentityAudit = isAuthenticated && user.HasClaim("permission", "identity.audit.read");
        canManageOidc = isAuthenticated && user.HasClaim("permission", "identity.oidc.manage");
        canAccessIdentity = canReadUsers || canReadRoles || canReadIdentityAudit || canManageOidc;
        isDebug = isAuthenticated && user.IsInRole("Debug");
    }

    private void ToggleMemberManagementExpanded() => isMemberManagementExpanded = !isMemberManagementExpanded;
    private void ToggleIdentityExpanded() => isIdentityExpanded = !isIdentityExpanded;
    private void ToggleInvoicesExpanded() => isInvoicesExpanded = !isInvoicesExpanded;
    private void ToggleDisbursementsExpanded() => isDisbursementsExpanded = !isDisbursementsExpanded;
    private void ToggleDebugExpanded() => isDebugExpanded = !isDebugExpanded;

    private Task NotifyNavigation()
    {
        return OnNavigate.HasDelegate ? OnNavigate.InvokeAsync() : Task.CompletedTask;
    }
}
