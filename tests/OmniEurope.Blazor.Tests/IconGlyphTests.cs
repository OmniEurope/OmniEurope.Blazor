using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed partial class IconGlyphTests : OmniBunitContext
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
    /// The package embeds exactly the outlines it renders itself. Consumers reach every other
    /// Phosphor icon through <see cref="OmniIcon.Glyph"/>, which costs the payload nothing, so a
    /// bulk import of the catalogue must fail here rather than in a consumer's download size.
    /// </summary>
    [Fact]
    public void EmbeddedOutlines_StayLimitedToTheBuiltInNames()
    {
        var embedded = PhosphorFactoryCall().Matches(Read("src", "OmniEurope.Blazor", "Internal", "PhosphorIconGlyphs.cs")).Count;

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

    [GeneratedRegex(@"OmniIconGlyph\.Phosphor\(")]
    private static partial Regex PhosphorFactoryCall();

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
