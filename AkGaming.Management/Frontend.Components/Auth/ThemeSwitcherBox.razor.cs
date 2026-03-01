using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Auth;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public enum Theme {
    System,
    Light,
    Dark
}

public partial class ThemeSwitcherBox : ComponentBase, IAsyncDisposable {
    [Inject] public IJSRuntime JS { get; set; } = default!;

    private Theme _currentTheme = Theme.System;
    private DotNetObjectReference<ThemeSwitcherBox>? _selfRef;

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (!firstRender) return;

        var saved = await JS.InvokeAsync<string>("themeApi.init");
        _currentTheme = saved switch {
            "light" => Theme.Light,
            "dark" => Theme.Dark,
            _ => Theme.System
        };

        // listen for system changes only when user chose 'system'
        if (_currentTheme == Theme.System) {
            _selfRef ??= DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("themeApi.onSystemChanged", _selfRef);
        }

        StateHasChanged();
    }

    private async Task SetTheme(Theme theme) {
        _currentTheme = theme;

        var jsValue = theme switch {
            Theme.Light => "light",
            Theme.Dark => "dark",
            _ => "system"
        };

        await JS.InvokeVoidAsync("themeApi.set", jsValue);

        // (Re)wire system-change callback only if 'system' is active
        if (theme == Theme.System) {
            _selfRef ??= DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("themeApi.onSystemChanged", _selfRef);
        }

        StateHasChanged();
    }

    [JSInvokable]
    public Task OnSystemThemeChanged() {
        StateHasChanged();
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync() {
        _selfRef?.Dispose();
        await Task.CompletedTask;
    }
}
