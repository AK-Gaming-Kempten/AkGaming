using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Membership;

public partial class NoMemberPanel : ComponentBase {
    private enum FormMode {
        None,
        RequestLink,
        Apply
    }

    [Parameter] public Guid UserId { get; set; }
    [Parameter] public MemberDto Member { get; set; } = default!;
    [Parameter] public IReadOnlyList<MemberLinkingRequestDto> LinkingRequests { get; set; } = [];
    [Parameter] public IReadOnlyList<MembershipApplicationRequestDto> ApplicationRequests { get; set; } = [];
    [Parameter] public bool ShowStatuses { get; set; }
    [Parameter] public bool CanCreateRequests { get; set; }
    [Parameter] public EventCallback<MemberDto> OnRequestSubmitted { get; set; }

    private FormMode _formMode;
    private bool HasPendingRequest => LinkingRequests.Any(x => !x.IsResolved) || ApplicationRequests.Any(x => !x.IsResolved);
    private string DialogTitle => _formMode == FormMode.Apply ? "Apply for membership" : "Request member linking";

    private void ShowRequestLink() => _formMode = FormMode.RequestLink;
    private void ShowApply() => _formMode = FormMode.Apply;
    private void CloseDialog() => _formMode = FormMode.None;

    private async Task HandleSubmittedAsync(MemberDto member) {
        CloseDialog();
        await OnRequestSubmitted.InvokeAsync(member);
    }

    private static string RequestDetails(MembershipApplicationRequestDto request) {
        var name = $"{request.MemberCreationInfo.FirstName} {request.MemberCreationInfo.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? "Application" : name;
    }
}
