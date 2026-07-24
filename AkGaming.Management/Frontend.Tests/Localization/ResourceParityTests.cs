using System.Xml.Linq;

namespace AkGaming.Management.Frontend.Tests.Localization;

[TestFixture]
public sealed class ResourceParityTests
{
    private static readonly string ResourcesDirectory = Path.GetFullPath(
        Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Frontend", "Resources", "Localization"));

    [Test]
    [Description("Every English localization resource has a German resource containing exactly the same keys.")]
    public void EnglishAndGermanResources_HaveMatchingKeys()
    {
        // Arrange
        var englishFiles = Directory.GetFiles(ResourcesDirectory, "*.en-GB.resx");

        // Act
        var mismatches = englishFiles
            .Select(CompareWithGermanResource)
            .Where(message => message is not null)
            .ToList();

        // Assert
        Assert.That(englishFiles, Is.Not.Empty);
        Assert.That(mismatches, Is.Empty, string.Join(Environment.NewLine, mismatches));
    }

    private static string? CompareWithGermanResource(string englishPath)
    {
        var germanPath = englishPath.Replace(".en-GB.resx", ".de-DE.resx", StringComparison.Ordinal);
        if (!File.Exists(germanPath))
        {
            return $"Missing German resource: {Path.GetFileName(germanPath)}";
        }

        var englishKeys = ReadKeys(englishPath);
        var germanKeys = ReadKeys(germanPath);
        var missingGerman = englishKeys.Except(germanKeys).Order().ToArray();
        var missingEnglish = germanKeys.Except(englishKeys).Order().ToArray();

        if (missingGerman.Length == 0 && missingEnglish.Length == 0)
        {
            return null;
        }

        return $"{Path.GetFileName(englishPath)}: missing German [{string.Join(", ", missingGerman)}], missing English [{string.Join(", ", missingEnglish)}]";
    }

    private static HashSet<string> ReadKeys(string path)
    {
        var document = XDocument.Load(path);
        var keys = document.Root!
            .Elements("data")
            .Select(element => (string?)element.Attribute("name"))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>();

        return new HashSet<string>(keys, StringComparer.Ordinal);
    }
}
