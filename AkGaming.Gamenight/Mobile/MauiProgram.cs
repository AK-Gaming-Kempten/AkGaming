using AkGaming.Gamenight.Shared;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.DependencyInjection;
namespace AkGaming.Gamenight.Mobile;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<MobileSession>();
        builder.Services.AddSingleton<IRegistrationExport, MobileRegistrationExport>();
        builder.Services.AddSingleton<IUserSession>(s => s.GetRequiredService<MobileSession>());
        builder.Services.AddSingleton(s => new GamenightClient(new HttpClient { BaseAddress = new Uri("https://gamenight.akgaming.de/") }, s.GetRequiredService<IUserSession>()));
        builder.Services.AddSingleton<MainPage>();
        return builder.Build();
    }
}
public sealed class App(MainPage page) : Application
{
    protected override Window CreateWindow(IActivationState? activationState) => new(page);
}
public sealed class MainPage : ContentPage
{
    public MainPage(MobileSession session)
    {
        var web = new BlazorWebView { HostPage = "wwwroot/index.html" };
        web.RootComponents.Add(new RootComponent { Selector = "#app", ComponentType = typeof(Routes) });
        Content = web;
        session.Changed += () => MainThread.BeginInvokeOnMainThread(() =>
        {
            web.RootComponents.Clear();
            web.RootComponents.Add(new RootComponent { Selector = "#app", ComponentType = typeof(Routes) });
        });
    }
}
