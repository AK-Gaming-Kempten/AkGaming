using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AkGaming.Core.Components.Auth;

public enum Theme
{
    System,
    Light,
    Dark
}

public partial class ThemeSwitcherBox : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private Theme _currentTheme = Theme.System;
    private DotNetObjectReference<ThemeSwitcherBox>? _selfRef;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        var saved = await JS.InvokeAsync<string>("themeApi.init");
        _currentTheme = saved switch
        {
            "light" => Theme.Light,
            "dark" => Theme.Dark,
            _ => Theme.System
        };

        if (_currentTheme == Theme.System)
            await EnsureSystemThemeCallbackAsync();

        StateHasChanged();
    }

    private async Task SetTheme(Theme theme)
    {
        _currentTheme = theme;

        var jsValue = theme switch
        {
            Theme.Light => "light",
            Theme.Dark => "dark",
            _ => "system"
        };

        await JS.InvokeVoidAsync("themeApi.set", jsValue);

        if (theme == Theme.System)
            await EnsureSystemThemeCallbackAsync();

        StateHasChanged();
    }

    private async Task EnsureSystemThemeCallbackAsync()
    {
        _selfRef ??= DotNetObjectReference.Create(this);
        await JS.InvokeVoidAsync("themeApi.onSystemChanged", _selfRef);
    }

    [JSInvokable]
    public Task OnSystemThemeChanged()
    {
        StateHasChanged();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _selfRef?.Dispose();
        return ValueTask.CompletedTask;
    }
}
