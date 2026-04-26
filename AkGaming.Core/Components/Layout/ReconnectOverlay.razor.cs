using Microsoft.AspNetCore.Components;

namespace AkGaming.Core.Components.Layout;

public partial class ReconnectOverlay : ComponentBase
{
    [Parameter] public string AppName { get; set; } = "AK Gaming";
    [Parameter] public string LogoUrl { get; set; } = "images/icons/AKG_Logos/Default.png";
    [Parameter] public string LogoAlt { get; set; } = "AK Gaming Logo";
    [Parameter] public string Title { get; set; } = "Server connection interrupted";
    [Parameter] public string ShowMessage { get; set; } = "Trying to restore your live session and reconnect to the server.";
    [Parameter] public string FailedMessage { get; set; } = "The last reconnect attempt failed. The app is still retrying automatically in the background.";
    [Parameter] public string FailedSubtleMessage { get; set; } = "If this keeps happening, reload the page to start a fresh interactive session.";
    [Parameter] public string RejectedMessage { get; set; } = "The server rejected the reconnect request. Your current session can no longer be resumed.";
    [Parameter] public string RejectedSubtleMessage { get; set; } = "Reload the page to open a new connection.";
    [Parameter] public string ReloadText { get; set; } = "Reload page";
}
