using System.Reflection;
using System.Text;

namespace AkGaming.InvoiceGenerator.Core.Rendering;

internal static class CoreThemeAssetLoader
{
    public static string LoadTextBySuffix(string suffix)
    {
        var bytes = LoadBytesBySuffix(suffix);
        return bytes.Length == 0 ? string.Empty : Encoding.UTF8.GetString(bytes);
    }

    public static byte[] LoadBytesBySuffix(string suffix)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
            return [];

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return [];

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
