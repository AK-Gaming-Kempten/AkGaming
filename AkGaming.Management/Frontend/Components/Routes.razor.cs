using AkGaming.Management.Frontend.Authentication;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components;

public partial class Routes : ComponentBase, IDisposable {
    [Inject] private FrontendSessionCoordinator SessionCoordinator { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized() {
        SessionCoordinator.SessionExpired += HandleSessionExpiredAsync;
    }

    private Task HandleSessionExpiredAsync() {
        var currentPath = Navigation.ToBaseRelativePath(Navigation.Uri);
        var returnUrl = string.IsNullOrWhiteSpace(currentPath) ? "/" : "/" + currentPath;
        var target = $"/authentication/login?returnUrl={Uri.EscapeDataString(returnUrl)}";
        return InvokeAsync(() => Navigation.NavigateTo(target, forceLoad: true));
    }

    public void Dispose() {
        SessionCoordinator.SessionExpired -= HandleSessionExpiredAsync;
    }
}
