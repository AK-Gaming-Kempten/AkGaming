using AkGaming.Gamenight.Shared;
namespace AkGaming.Gamenight.Mobile;
public sealed class MobileRegistrationExport : IRegistrationExport
{
    public async Task SaveAsync(string name, string csv)
    {
        var path = Path.Combine(FileSystem.CacheDirectory, Path.GetFileName(name));
        await File.WriteAllTextAsync(path, csv, System.Text.Encoding.UTF8);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Game-Night-Anmeldungen exportieren",
            File = new ShareFile(path, "text/csv")
        });
    }
}
