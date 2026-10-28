using System.Text.RegularExpressions;
using Bunit;
using OmniEurope.Blazor.Showcase.Components.Pages;
using OmniEurope.Blazor.Showcase.Demos;
using OmniEurope.Blazor.Showcase.Gallery;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Holds the gallery addresses to the catalogues: every written link lands on a real page of the
/// gallery or on a real legacy demo, and an address naming no entry reads as not found instead of
/// showing another demo.
/// </summary>
public sealed partial class ShowcaseGalleryRoutingTests : OmniBunitContext
{
    private static readonly string[] SiteExtensions = [".razor", ".cs", ".html", ".js", ".json", ".resx"];

    /// <summary>The segment under which the legacy demos stay reachable until they all move to pages.</summary>
    private const string Legacy = "legacy";

    [Fact]
    public void EveryWrittenGalleryLink_NamesACatalogueEntry()
    {
        var root = ShippedLookTests.RepositoryRoot();
        var site = Path.Combine(root, "site");
        var files = Directory.EnumerateFiles(site, "*", SearchOption.AllDirectories)
            .Where(path => SiteExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Where(path => !IsBuildOutput(Path.GetRelativePath(site, path)))
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "eng"), "*.mjs", SearchOption.AllDirectories))
            .ToArray();
        Assert.NotEmpty(files);

        var slugs = GalleryCatalog.Pages.Select(page => page.Slug).ToHashSet(StringComparer.Ordinal);
        var keys = DemoCatalog.All.Select(demo => demo.Key).ToHashSet(StringComparer.Ordinal);
        var pageLinks = 0;
        var legacyLinks = 0;
        var dead = new List<string>();
        foreach (var file in files)
        {
            foreach (Match link in GalleryLink().Matches(File.ReadAllText(file)))
            {
                var legacy = link.Groups["legacy"].Success;
                var known = legacy ? keys.Contains(link.Groups["key"].Value) : slugs.Contains(link.Groups["key"].Value);
                if (legacy)
                {
                    legacyLinks++;
                }
                else
                {
                    pageLinks++;
                }

                if (!known)
                {
                    dead.Add($"{Path.GetRelativePath(root, file)}: {link.Value}");
                }
            }
        }

        // The scan must have found the links it guards, or a broken pattern would pass silently.
        Assert.True(pageLinks > 0, "No literal gallery page link was found; the scan pattern no longer matches the sources.");
        Assert.True(legacyLinks > 0, "No literal legacy demo link was found; the scan pattern no longer matches the sources.");
        Assert.True(dead.Count == 0, $"Gallery links naming no catalogue entry:{Environment.NewLine}{string.Join(Environment.NewLine, dead)}");
    }

    [Fact]
    public void NoOldFrenchRouteIsLeft()
    {
        var root = ShippedLookTests.RepositoryRoot();
        var files = Directory.EnumerateFiles(Path.Combine(root, "site"), "*", SearchOption.AllDirectories)
            .Where(path => SiteExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Where(path => !IsBuildOutput(Path.GetRelativePath(Path.Combine(root, "site"), path)))
            .Where(path => Path.GetExtension(path) != ".resx")
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "eng"), "*.mjs", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "eng"), "*.ps1", SearchOption.AllDirectories));

        var stale = files
            .Where(file => OldRoute().IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        Assert.True(stale.Length == 0, $"Old French routes are still written in: {string.Join(", ", stale)}");
    }

    [Fact]
    public void UnknownLegacyKey_RendersNotFound_NotTheFirstDemo()
    {
        var page = Render<ComponentGallery>(parameters => parameters.Add(p => p.DemoKey, "unknown"));

        Assert.NotNull(page.Find("#not-found-title"));
        Assert.Empty(page.FindAll("#demo-title"));
        Assert.Empty(page.FindAll(".showcase-preview"));
    }

    [Fact]
    public void BareLegacyAddress_ShowsTheFirstEntry()
    {
        var page = Render<ComponentGallery>();

        Assert.Empty(page.FindAll("#not-found-title"));
        Assert.NotNull(page.Find("#demo-title"));
    }

    [Fact]
    public void Find_ReturnsNullForAnUnknownKey_AndResolveRefusesIt()
    {
        Assert.Null(DemoCatalog.Find("unknown"));
        Assert.Null(DemoCatalog.Find(null));
        Assert.Same(DemoCatalog.All[0], DemoCatalog.Find(DemoCatalog.All[0].Key));
        Assert.Throws<ArgumentException>(() => DemoCatalog.Resolve("unknown"));

        Assert.Null(GalleryCatalog.Find("unknown"));
        Assert.Null(GalleryCatalog.Find(null));
        Assert.Throws<ArgumentException>(() => GalleryCatalog.Resolve("unknown"));
        Assert.All(GalleryCatalog.Pages, page => Assert.Same(page, GalleryCatalog.Find(page.Slug)));
    }

    [Fact]
    public void LegacyKeysAndPageSlugs_NeverCollideWithTheLegacySegment()
    {
        Assert.DoesNotContain(Legacy, GalleryCatalog.Pages.Select(page => page.Slug));
        Assert.Equal(GalleryCatalog.Pages.Count(), GalleryCatalog.Pages.Select(page => page.Slug).Distinct(StringComparer.Ordinal).Count());
    }

    private static bool IsBuildOutput(string relative) =>
        relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(segment => segment is "bin" or "obj");

    /// <summary>
    /// A literal gallery address: <c>components/</c> then a lowercase slug, or
    /// <c>components/legacy/</c> then a legacy key. An interpolated key (<c>components/{page.Slug}</c>,
    /// <c>components/${key}</c>) starts with a brace or a dollar and is not a literal, so it is left out.
    /// </summary>
    [GeneratedRegex(@"(?<![A-Za-z0-9_.-])components/(?:(?<legacy>legacy)/)?(?<key>(?!legacy\b)[a-z0-9]+(?:-[a-z0-9]+)*)")]
    private static partial Regex GalleryLink();

    /// <summary>The addresses the showcase used before its routes were written in English.</summary>
    [GeneratedRegex(@"['""/]composants(?:/|['""])|/personnalisation\b|""personnalisation""")]
    private static partial Regex OldRoute();
}
