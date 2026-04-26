using Microsoft.AspNetCore.Components;

namespace AkGaming.Core.Components.Auth;

public partial class UnauthorizedPanel : ComponentBase
{
    [Parameter] public string? ImageUrl { get; set; }
    [Parameter] public string ImageAlt { get; set; } = "Not authorized";
    [Parameter] public string LoginText { get; set; } = "Login";
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Inject] private NavigationManager Nav { get; set; } = default!;

    private void Login() => Nav.NavigateTo("/authentication/login", forceLoad: true);
}
