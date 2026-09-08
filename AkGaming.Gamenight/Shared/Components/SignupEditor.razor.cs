using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class SignupEditor
{
    [Parameter, EditorRequired] public SignupForm Form { get; set; } = default!;
    [Parameter] public bool Editing { get; set; }
    [Parameter] public bool Busy { get; set; }
    [Parameter] public EventCallback Submit { get; set; }
    private int _step;
    private void Next() => _step++;
    private void Back() => _step--;
    private void ToggleSource(string source, ChangeEventArgs e)
    {
        if (e.Value is true && !Form.DiscoverySources.Contains(source)) Form.DiscoverySources.Add(source);
        else Form.DiscoverySources.Remove(source);
    }
}

