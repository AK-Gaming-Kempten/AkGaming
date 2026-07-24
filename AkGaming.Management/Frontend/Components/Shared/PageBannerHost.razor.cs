using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace AkGaming.Management.Frontend.Components.Shared;

public partial class PageBannerHost : ComponentBase, IDisposable
{
    private static readonly BannerDefinition FallbackBanner = new(
        "bi-grid",
        "Page_Management_Title",
        "Page_Management_Description");

    private static readonly IReadOnlyList<RouteBanner> Routes =
    [
        Exact("", "bi-house-door-fill", "Page_Home_Title", "Page_Home_Description"),
        Exact("account/accessdenied", "bi-shield-lock", "Page_AccessDenied_Title", "Page_AccessDenied_Description"),
        Exact("error", "bi-exclamation-triangle", "Page_Error_Title", "Page_Error_Description"),
        Exact("debug", "bi-bug", "Page_Debug_Title", "Page_Debug_Description"),
        Exact("membership", "bi-person-fill", "Page_Profile_Title", "Page_Profile_Description"),
        Prefix("membership/user/", "bi-person-fill", "Page_Profile_Title", "Page_Profile_Description"),
        Exact("general-meetings", "bi-people", "Page_GeneralMeetings_Title", "Page_GeneralMeetings_Description"),
        Prefix("general-meetings/", "bi-people-fill", "Page_GeneralMeeting_Title", "Page_GeneralMeeting_Description"),
        Exact("board/meetings", "bi-calendar2-week", "Page_BoardMeetings_Title", "Page_BoardMeetings_Description"),
        Prefix("board/meetings/", "bi-calendar2-event", "Page_BoardMeeting_Title", "Page_BoardMeeting_Description"),
        Exact("disbursements/reimbursements/my", "bi-receipt-cutoff", "Page_MyReimbursements_Title", "Page_MyReimbursements_Description"),
        Exact("disbursements/allocations/my", "bi-trophy", "Page_MyAllocations_Title", "Page_MyAllocations_Description"),
        Prefix("disbursements/claim/", "bi-trophy-fill", "Page_AllocationClaim_Title", "Page_AllocationClaim_Description"),
        Exact("disbursements/admin/reimbursements", "bi-clipboard-check", "Page_ReimbursementsAdmin_Title", "Page_ReimbursementsAdmin_Description"),
        Exact("disbursements/admin/events", "bi-calendar-event", "Page_DisbursementEvents_Title", "Page_DisbursementEvents_Description"),
        Prefix("disbursements/admin/events/", "bi-calendar-event-fill", "Page_DisbursementEvent_Title", "Page_DisbursementEvent_Description"),
        Exact("member-management/members", "bi-people-fill", "Page_Members_Title", "Page_Members_Description"),
        Exact("member-management/trial", "bi-hourglass-split", "Page_TrialMembers_Title", "Page_TrialMembers_Description"),
        Exact("member-management/requests", "bi-envelope-fill", "Page_MemberRequests_Title", "Page_MemberRequests_Description"),
        Exact("member-management/dues", "bi-cash-stack", "Page_MemberDues_Title", "Page_MemberDues_Description"),
        Exact("member-management/audit-log", "bi-journal-text", "Page_MemberAudit_Title", "Page_MemberAudit_Description"),
        Exact("invoices/manage", "bi-file-earmark-text", "Page_Invoices_Title", "Page_Invoices_Description"),
        Exact("invoices/presets", "bi-collection", "Page_InvoicePresets_Title", "Page_InvoicePresets_Description"),
        Exact("invoices/parties", "bi-buildings", "Page_InvoiceParties_Title", "Page_InvoiceParties_Description"),
        Exact("invoices/payment-terms", "bi-calendar-check", "Page_InvoicePaymentTerms_Title", "Page_InvoicePaymentTerms_Description"),
        Exact("identity/clients", "bi-window-stack", "Page_IdentityClients_Title", "Page_IdentityClients_Description"),
        Exact("identity/scopes", "bi-diagram-3", "Page_IdentityScopes_Title", "Page_IdentityScopes_Description"),
        Exact("identity/roles", "bi-person-badge-fill", "Page_IdentityRoles_Title", "Page_IdentityRoles_Description"),
        Exact("identity/users", "bi-person-gear", "Page_IdentityUsers_Title", "Page_IdentityUsers_Description"),
        Exact("identity/audit-log", "bi-journal-text", "Page_IdentityAudit_Title", "Page_IdentityAudit_Description")
    ];

    private BannerDefinition? CurrentBanner { get; set; }

    protected override void OnInitialized()
    {
        Navigation.LocationChanged += OnLocationChanged;
        ResolveCurrentBanner();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        ResolveCurrentBanner();
        _ = InvokeAsync(StateHasChanged);
    }

    private void ResolveCurrentBanner()
    {
        var relativePath = Navigation.ToBaseRelativePath(Navigation.Uri);
        var queryIndex = relativePath.IndexOfAny(['?', '#']);
        var normalizedPath = (queryIndex >= 0 ? relativePath[..queryIndex] : relativePath).Trim('/').ToLowerInvariant();
        var route = Routes.FirstOrDefault(candidate => candidate.IsMatch(normalizedPath));
        CurrentBanner = route?.Banner ?? FallbackBanner;
    }

    private static RouteBanner Exact(string path, string icon, string titleKey, string descriptionKey)
    {
        var banner = new BannerDefinition(icon, titleKey, descriptionKey);
        return new RouteBanner(candidate => string.Equals(candidate, path, StringComparison.Ordinal), banner);
    }

    private static RouteBanner Prefix(string prefix, string icon, string titleKey, string descriptionKey)
    {
        var banner = new BannerDefinition(icon, titleKey, descriptionKey);
        return new RouteBanner(candidate => candidate.StartsWith(prefix, StringComparison.Ordinal), banner);
    }

    public void Dispose()
    {
        Navigation.LocationChanged -= OnLocationChanged;
    }

    private sealed record BannerDefinition(string Icon, string TitleKey, string DescriptionKey);
    private sealed record RouteBanner(Func<string, bool> IsMatch, BannerDefinition Banner);
}
