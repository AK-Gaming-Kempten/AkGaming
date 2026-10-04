using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Layout;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private bool _isMobileNavOpen;
    private bool IsHomeRoute => string.IsNullOrWhiteSpace(Nav.ToBaseRelativePath(Nav.Uri));

    private void ToggleMobileNav()
    {
        _isMobileNavOpen = !_isMobileNavOpen;
    }

    private void CloseMobileNav()
    {
        _isMobileNavOpen = false;
    }

    private void CloseMobileNavIfOpen()
    {
        if (_isMobileNavOpen)
        {
            _isMobileNavOpen = false;
        }
    }
}
