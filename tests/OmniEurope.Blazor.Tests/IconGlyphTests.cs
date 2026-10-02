using System.Text.Json;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class IconGlyphTests : OmniBunitContext
{
    [Fact]
    public void EveryBuiltInName_RendersADistinctPhosphorOutline()
    {
        var outlines = new Dictionary<string, OmniIconName>(StringComparer.Ordinal);

        foreach (var name in Enum.GetValues<OmniIconName>())
        {
            var svg = Render<OmniIcon>(parameters => parameters.Add(item => item.Name, name)).Find("svg");
            var path = svg.QuerySelector("path")?.GetAttribute("d");

            Assert.False(string.IsNullOrWhiteSpace(path), $"{name} rendu sans tracé.");
            Assert.Equal("0 0 256 256", svg.GetAttribute("viewBox"));
            Assert.Equal("currentColor", svg.GetAttribute("fill"));
            Assert.True(outlines.TryAdd(path!, name), $"{name} réutilise le tracé de {outlines.GetValueOrDefault(path!)}.");
        }
    }

    [Theory]
    [InlineData(OmniIconName.FilePdf, "omni-icon--format-pdf")]
    [InlineData(OmniIconName.FileDoc, "omni-icon--format-doc")]
    [InlineData(OmniIconName.FileXls, "omni-icon--format-sheet")]
    [InlineData(OmniIconName.FileCsv, "omni-icon--format-sheet")]
    [InlineData(OmniIconName.FileMd, "omni-icon--format-text")]
    public void FormatColor_TintsAFileIconWithItsFormat_OnlyWhenAsked(OmniIconName name, string formatClass)
    {
        // recette R-051: the glyph takes its format colour, the button around it keeps its variant.
        var tinted = Render<OmniIcon>(parameters => parameters.Add(item => item.Name, name).Add(item => item.FormatColor, true)).Find("svg");
        var plain = Render<OmniIcon>(parameters => parameters.Add(item => item.Name, name)).Find("svg");

        Assert.Contains(formatClass, tinted.ClassList);
        Assert.DoesNotContain(plain.ClassList, value => value.StartsWith("omni-icon--format-", StringComparison.Ordinal));
        var other = Render<OmniIcon>(parameters => parameters.Add(item => item.Name, OmniIconName.Check).Add(item => item.FormatColor, true)).Find("svg");
        Assert.DoesNotContain(other.ClassList, value => value.StartsWith("omni-icon--format-", StringComparison.Ordinal));
        Assert.Matches(@"\.omni-icon--format-doc \{ color: var\(--omni-color-info\); \}", StylesheetSource.Read());
    }

    [Fact]
    public void ExplicitGlyph_WinsOverName()
    {
        const string Outline = "M0,0L16,16Z";

        var component = Render<OmniIcon>(parameters => parameters
            .Add(item => item.Name, OmniIconName.Check)
            .Add(item => item.Glyph, new OmniIconGlyph(Outline, 16)));

        Assert.Equal(Outline, component.Find("path").GetAttribute("d"));
        Assert.Equal("0 0 16 16", component.Find("svg").GetAttribute("viewBox"));
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("M0,0\"/><script>alert(1)</script>")]
    [InlineData("url(#gradient)")]
    public void Glyph_RejectsAnythingThatIsNotAnOutline(string pathData) =>
        Assert.Throws<ArgumentException>(() => OmniIconGlyph.Phosphor(pathData));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Glyph_RejectsEmptyOutline(string pathData) =>
        Assert.Throws<ArgumentException>(() => OmniIconGlyph.Phosphor(pathData));

    [Fact]
    public void Glyph_RejectsNonPositiveViewBox() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new OmniIconGlyph("M0,0Z", 0));

    /// <summary>
    /// The package embeds a chosen catalogue: one outline per <see cref="OmniIconName"/> value, the
    /// icons its applications display and nothing more. Each icon is added by name, so a bulk
    /// import of the Phosphor set must fail here rather than in a consumer's download size.
    /// </summary>
    [Fact]
    public void EmbeddedOutlines_StayLimitedToTheBuiltInNames()
    {
        var embedded = File
            .ReadAllLines(Path.Combine(Root, "src", "OmniEurope.Blazor", "Internal", "PhosphorIcons.txt"))
            .Count(line => line.Length > 0 && line[0] != '#');

        Assert.Equal(Enum.GetValues<OmniIconName>().Length, embedded);
    }

    [Fact]
    public void VendoredManifest_DeclaresThePhosphorProvenance()
    {
        using var manifest = JsonDocument.Parse(Read("eng", "vendored-assets.json"));

        var phosphor = manifest.RootElement
            .GetProperty("assets")
            .EnumerateArray()
            .Single(asset => asset.GetProperty("id").GetString() == "phosphor-icons");

        Assert.Equal("MIT", phosphor.GetProperty("license").GetProperty("value").GetString());
        Assert.Contains(
            "src/OmniEurope.Blazor/Internal/PhosphorIconGlyphs.cs",
            phosphor.GetProperty("vendoredFiles").EnumerateArray().Select(file => file.GetString()));

        var licenseFile = phosphor.GetProperty("license").GetProperty("localFile").GetString()!;
        Assert.True(File.Exists(Path.Combine(Root, licenseFile)), $"Texte de licence absent : {licenseFile}");
    }

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OmniEurope.Blazor.slnx")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("Racine du dépôt introuvable.");
        }
    }

    private static string Read(params string[] segments) => File.ReadAllText(Path.Combine([Root, .. segments]));
}
