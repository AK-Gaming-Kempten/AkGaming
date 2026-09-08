using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class GameNightFields
{
    [Parameter, EditorRequired] public SignupForm Form { get; set; } = default!;
}

