using AkGaming.Gamenight.Shared;
using Microsoft.JSInterop;
namespace AkGaming.Gamenight.Frontend;
public sealed class BrowserRegistrationExport(IJSRuntime js) : IRegistrationExport
{
    public async Task SaveAsync(string name, string csv)
    {
        await js.InvokeVoidAsync("gamenight.download", name, csv);
    }
}
