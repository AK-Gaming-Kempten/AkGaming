using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.GeneralMeetings;

public partial class LiveMinutesEditor : ComponentBase, IDisposable
{
    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> OnSave { get; set; }
    private string? _value;
    private string? _lastReceivedValue;
    private CancellationTokenSource? _saveDelay;
    protected override void OnParametersSet() { if (!string.Equals(Value, _lastReceivedValue, StringComparison.Ordinal)) { _value = Value; _lastReceivedValue = Value; } }
    private void HandleInput(ChangeEventArgs args)
    {
        _value = args.Value?.ToString();
        _saveDelay?.Cancel(); _saveDelay?.Dispose(); _saveDelay = new CancellationTokenSource();
        _ = SaveAfterDelayAsync(_value, _saveDelay.Token);
    }
    private async Task SaveAfterDelayAsync(string? value, CancellationToken cancellationToken)
    {
        try { await Task.Delay(600, cancellationToken); await InvokeAsync(() => OnSave.InvokeAsync(value)); }
        catch (OperationCanceledException) { }
    }
    public void Dispose() { _saveDelay?.Cancel(); _saveDelay?.Dispose(); }
}
