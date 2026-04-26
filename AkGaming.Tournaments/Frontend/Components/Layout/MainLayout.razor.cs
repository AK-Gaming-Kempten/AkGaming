using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Layout;

public partial class MainLayout
{
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private bool _isMobileNavOpen;
    private bool IsHomeRoute => string.IsNullOrWhiteSpace(Nav.ToBaseRelativePath(Nav.Uri));

    private void ToggleMobileNav() => _isMobileNavOpen = !_isMobileNavOpen;

    private void CloseMobileNav() => _isMobileNavOpen = false;

    private void CloseMobileNavIfOpen()
    {
        if (_isMobileNavOpen)
            _isMobileNavOpen = false;
    }
}
