using Microsoft.AspNetCore.Components;

namespace AkGaming.Core.Components.Feedback;

public partial class Toast : ComponentBase, IAsyncDisposable
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public string? Message { get; set; }
    [Parameter] public string AppName { get; set; } = "AK Gaming";
    [Parameter] public string Title { get; set; } = "Notification";
    [Parameter] public ToastVariant Variant { get; set; } = ToastVariant.Error;
    [Parameter] public int AutoDismissMilliseconds { get; set; } = 8000;
    [Parameter] public EventCallback OnDismiss { get; set; }

    private CancellationTokenSource? _dismissCancellation;
    private string? _scheduledMessage;

    private string IconClass => Variant switch
    {
        ToastVariant.Success => "bi-check-circle",
        ToastVariant.Warning => "bi-exclamation-triangle",
        ToastVariant.Info => "bi-info-circle",
        _ => "bi-exclamation-octagon"
    };

    private Task DismissAsync()
    {
        CancelAutoDismiss();
        return OnDismiss.InvokeAsync();
    }

    protected override void OnParametersSet()
    {
        if (!Visible || string.IsNullOrWhiteSpace(Message) || AutoDismissMilliseconds <= 0)
        {
            CancelAutoDismiss();
            _scheduledMessage = null;
            return;
        }

        if (string.Equals(_scheduledMessage, Message, StringComparison.Ordinal))
            return;

        CancelAutoDismiss();
        _scheduledMessage = Message;
        _dismissCancellation = new CancellationTokenSource();
        _ = AutoDismissAsync(_dismissCancellation.Token);
    }

    private async Task AutoDismissAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(AutoDismissMilliseconds, cancellationToken);
            await InvokeAsync(DismissAsync);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void CancelAutoDismiss()
    {
        _dismissCancellation?.Cancel();
        _dismissCancellation?.Dispose();
        _dismissCancellation = null;
    }

    public ValueTask DisposeAsync()
    {
        CancelAutoDismiss();
        return ValueTask.CompletedTask;
    }
}

public enum ToastVariant
{
    Error,
    Warning,
    Info,
    Success
}
