namespace AkGaming.Management.Frontend.Authentication;

public sealed class FrontendSessionCoordinator {
    private int _sessionExpiredRaised;

    public event Func<Task>? SessionExpired;

    public Task NotifySessionExpiredAsync() {
        if (Interlocked.Exchange(ref _sessionExpiredRaised, 1) != 0)
            return Task.CompletedTask;

        var handlers = SessionExpired;
        if (handlers is null)
            return Task.CompletedTask;

        return handlers.Invoke();
    }
}
