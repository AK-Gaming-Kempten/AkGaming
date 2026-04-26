using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class UnauthorizedPanel : ComponentBase
{
    [Parameter] public string? ImageUrl { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Inject] private NavigationManager Nav { get; set; } = default!;

    private void Login() => Nav.NavigateTo("/authentication/login", forceLoad: true);
}
