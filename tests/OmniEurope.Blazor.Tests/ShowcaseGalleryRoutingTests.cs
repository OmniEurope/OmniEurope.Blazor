using System.Text.RegularExpressions;
using Bunit;
using OmniEurope.Blazor.Showcase.Components.Pages;
using OmniEurope.Blazor.Showcase.Demos;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Holds the gallery addresses to the catalogue: every written link lands on a real entry, and an
/// address naming no entry reads as not found instead of showing another demo.
/// </summary>
public sealed partial class ShowcaseGalleryRoutingTests : OmniBunitContext
{
    private static readonly string[] SiteExtensions = [".razor", ".cs", ".html", ".js", ".json", ".resx"];

    [Fact]
    public void EveryWrittenGalleryLink_NamesACatalogueEntry()
    {
        var root = ShippedLookTests.RepositoryRoot();
        var site = Path.Combine(root, "site", "OmniEurope.Blazor.Showcase");
        var files = Directory.EnumerateFiles(site, "*", SearchOption.AllDirectories)
            .Where(path => SiteExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Where(path => !IsBuildOutput(Path.GetRelativePath(site, path)))
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "eng"), "*.mjs"))
            .ToArray();
        Assert.NotEmpty(files);

        var keys = DemoCatalog.All.Select(demo => demo.Key).ToHashSet(StringComparer.Ordinal);
        var links = 0;
        var dead = new List<string>();
        foreach (var file in files)
        {
            foreach (Match link in GalleryLink().Matches(File.ReadAllText(file)))
            {
                links++;
                if (!keys.Contains(link.Groups["key"].Value))
                {
                    dead.Add($"{Path.GetRelativePath(root, file)}: {link.Value}");
                }
            }
        }

        // The scan must have found the links it guards, or a broken pattern would pass silently.
        Assert.True(links > 0, "No literal gallery link was found; the scan pattern no longer matches the sources.");
        Assert.True(dead.Count == 0, $"Gallery links naming no catalogue entry:{Environment.NewLine}{string.Join(Environment.NewLine, dead)}");
    }

    [Fact]
    public void UnknownGalleryKey_RendersNotFound_NotTheFirstDemo()
    {
        var page = Render<ComponentGallery>(parameters => parameters.Add(p => p.DemoKey, "introuvable"));

        Assert.NotNull(page.Find("#not-found-title"));
        Assert.Empty(page.FindAll("#demo-title"));
        Assert.Empty(page.FindAll(".showcase-preview"));
    }

    [Fact]
    public void BareGalleryAddress_ShowsTheFirstEntry()
    {
        var page = Render<ComponentGallery>();

        Assert.Empty(page.FindAll("#not-found-title"));
        Assert.NotNull(page.Find("#demo-title"));
    }

    [Fact]
    public void Find_ReturnsNullForAnUnknownKey_AndResolveRefusesIt()
    {
        Assert.Null(DemoCatalog.Find("introuvable"));
        Assert.Null(DemoCatalog.Find(null));
        Assert.Same(DemoCatalog.All[1], DemoCatalog.Find(DemoCatalog.All[1].Key));
        Assert.Throws<ArgumentException>(() => DemoCatalog.Resolve("introuvable"));
    }

    private static bool IsBuildOutput(string relative)
    {
        var first = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        return first is "bin" or "obj";
    }

    /// <summary>
    /// A literal gallery address: <c>composants/</c> then a lowercase slug. An interpolated key
    /// (<c>composants/{demo.Key}</c>, <c>composants/${key}</c>) starts with a brace or a dollar and is
    /// not a literal, so it is left out.
    /// </summary>
    [GeneratedRegex(@"composants/(?<key>[a-z0-9]+(?:-[a-z0-9]+)*)")]
    private static partial Regex GalleryLink();
}
