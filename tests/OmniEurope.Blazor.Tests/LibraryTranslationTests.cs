using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace OmniEurope.Blazor.Tests;

public sealed class LibraryTranslationTests
{
    // The package ships its texts in the 24 official languages of the European Union: French is the
    // neutral file, the 23 others are satellites. A key added to the neutral file without its
    // translations would show French inside a German or Greek page.
    private static readonly string[] Cultures =
    [
        "bg", "cs", "da", "de", "el", "en", "es", "et", "fi", "ga", "hr", "hu",
        "it", "lt", "lv", "mt", "nl", "pl", "pt", "ro", "sk", "sl", "sv"
    ];

    [Fact]
    public void Every_EU_language_has_a_translation_file()
    {
        var missing = Cultures.Where(culture => !File.Exists(ResourcePath(culture))).ToArray();

        Assert.Empty(missing);
    }

    [Theory]
    [MemberData(nameof(CultureData))]
    public void Each_translation_has_exactly_the_neutral_keys_and_placeholders(string culture)
    {
        var neutral = Entries(ResourcePath(null));
        var translated = Entries(ResourcePath(culture));

        var missing = neutral.Keys.Except(translated.Keys).ToArray();
        var extra = translated.Keys.Except(neutral.Keys).ToArray();
        var placeholders = neutral.Keys.Intersect(translated.Keys)
            .Where(key => Placeholders(neutral[key]) != Placeholders(translated[key]))
            .ToArray();
        var empty = translated.Where(entry => entry.Value.Trim().Length == 0 && neutral.TryGetValue(entry.Key, out var source) && source.Trim().Length > 0)
            .Select(entry => entry.Key)
            .ToArray();

        Assert.True(
            missing.Length == 0 && extra.Length == 0 && placeholders.Length == 0 && empty.Length == 0,
            $"AppStrings.{culture}.resx: missing [{string.Join(", ", missing)}], extra [{string.Join(", ", extra)}], placeholders [{string.Join(", ", placeholders)}], empty [{string.Join(", ", empty)}].");
    }

    public static TheoryData<string> CultureData() => new(Cultures);

    private static string ResourcePath(string? culture)
    {
        var name = culture is null ? "AppStrings.resx" : $"AppStrings.{culture}.resx";
        return Path.Combine(RepositoryRoot(), "src", "OmniEurope.Blazor", "Resources", name);
    }

    private static Dictionary<string, string> Entries(string path) =>
        XDocument.Load(path).Root!.Elements("data")
            .ToDictionary(data => (string)data.Attribute("name")!, data => (string?)data.Element("value") ?? string.Empty);

    // The arguments a text names, each once: a plural block repeats its number in every form, and a
    // language may need a block where another does not (PluralTextTests checks the forms themselves).
    private static string Placeholders(string value) =>
        string.Join(",", Regex.Matches(value, @"\{\s*(\d+)\s*(?:[:,][^}]*)?\}").Select(match => match.Groups[1].Value).Distinct().Order());

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OmniEurope.Blazor.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
