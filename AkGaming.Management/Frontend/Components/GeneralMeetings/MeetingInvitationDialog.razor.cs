using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.GeneralMeetings.Contracts;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.GeneralMeetings;

public partial class MeetingInvitationDialog : ComponentBase
{
    [Parameter] public Guid MeetingId { get; set; }
    [Parameter] public bool IsReminder { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnDispatched { get; set; }
    [Inject] private GeneralMeetingsApiClient Api { get; set; } = null!;

    private InvitationPreviewDto? _preview;
    private string? _invitationText;
    private string? _error;
    private bool _loading;
    private bool _sending;
    private bool _previewDirty;
    private int EligibleRecipientCount => _preview?.Recipients.Count(recipient => recipient.WillReceive) ?? 0;
    private bool CanDispatch => _preview is not null && EligibleRecipientCount > 0 && !_previewDirty && !_loading && !_sending;

    protected override async Task OnInitializedAsync()
    {
        await LoadPreviewAsync();
    }

    private async Task LoadPreviewAsync()
    {
        _loading = true;
        _error = null;
        var result = await Api.PreviewInvitationAsync(MeetingId, IsReminder, _invitationText);
        _preview = result.IsSuccess ? result.Value : null;
        _error = result.IsSuccess ? null : result.Error;
        if (result.IsSuccess) _invitationText = result.Value!.InvitationText;
        _previewDirty = false;
        _loading = false;
    }

    private void MarkPreviewDirty()
    {
        _previewDirty = true;
    }

    private async Task DispatchAsync()
    {
        if (!CanDispatch) return;
        _sending = true;
        _error = null;
        var result = await Api.DispatchInvitationsAsync(MeetingId, IsReminder, _invitationText);
        _sending = false;
        if (!result.IsSuccess)
        {
            _error = result.Error;
            return;
        }

        await OnDispatched.InvokeAsync();
    }

    private Task CloseAsync()
    {
        return OnClose.InvokeAsync();
    }
}
