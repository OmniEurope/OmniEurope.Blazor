using System.Reflection;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Showcase.Examples;
using OmniEurope.Blazor.Showcase.Gallery;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Holds the gallery (PLAN-005) to its contract: every page renders on its three views in both
/// languages, every source its Code tab lists is the embedded file the examples project compiles,
/// every example is shown by a page, and the examples depend on the library alone.
/// </summary>
public sealed partial class ShowcaseGalleryTests : OmniBunitContext
{
    private static readonly string[] Cultures = ["en-IE", "fr-FR"];

    private static readonly string[] Views = ["preview", "mobile", "code"];

    public ShowcaseGalleryTests()
    {
        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("https://localhost/") });
        Services.AddScoped<ThemeTokenReader>();
        Services.AddScoped<ThemeState>();
    }

    public static TheoryData<string, string, string> PageViews
    {
        get
        {
            var data = new TheoryData<string, string, string>();
            foreach (var page in GalleryCatalog.Pages)
            {
                foreach (var culture in Cultures)
                {
                    foreach (var view in Views)
                    {
                        data.Add(page.Slug, culture, view);
                    }
                }
            }

            return data;
        }
    }

    [Fact]
    public void TheCatalogue_HasPages_WithUniqueSlugsAndARouteEach()
    {
        Assert.NotEmpty(GalleryCatalog.Pages);
        Assert.Equal(14, GalleryCatalog.Families.Count);
        foreach (var page in GalleryCatalog.Pages)
        {
            Assert.Matches(Slug(), page.Slug);
            Assert.NotEmpty(page.SourceFiles);
            Assert.NotNull(PageComponent(page));
        }
    }

    [Fact]
    public void EveryCatalogueSourceFile_IsAnEmbeddedExampleSource()
    {
        var missing = GalleryCatalog.Pages
            .SelectMany(page => page.SourceFiles.Select(path => (page.Slug, path)))
            .Where(entry => ExampleSource.Read(entry.path) is null)
            .Select(entry => $"{entry.Slug}: {entry.path}")
            .ToArray();

        Assert.True(missing.Length == 0, $"Source files not embedded in the examples assembly: {string.Join(", ", missing)}");
    }

    [Fact]
    public void EveryExampleFileOnDisk_IsEmbeddedUnderItsRelativePath()
    {
        var root = ExamplesFolder();
        var onDisk = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".razor" or ".cs" or ".css")
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Where(path => path.Contains('/', StringComparison.Ordinal) && !path.StartsWith("bin/", StringComparison.Ordinal) && !path.StartsWith("obj/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(onDisk);
        Assert.Equal(onDisk, ExampleSource.EmbeddedPaths);
        Assert.All(onDisk, path => Assert.Equal(File.ReadAllText(Path.Combine(root, path)).TrimEnd(), ExampleSource.Read(path)));
    }

    [Fact]
    public void EveryExample_IsListedByAtLeastOnePage()
    {
        var listed = GalleryCatalog.Pages.SelectMany(page => page.SourceFiles).ToHashSet(StringComparer.Ordinal);
        var orphans = ExampleSource.EmbeddedPaths
            .Where(path => path.EndsWith("Example.razor", StringComparison.Ordinal) && !listed.Contains(path))
            .ToArray();

        Assert.Contains(ExampleSource.EmbeddedPaths, path => path.EndsWith("Example.razor", StringComparison.Ordinal));
        Assert.True(orphans.Length == 0, $"Examples no gallery page shows: {string.Join(", ", orphans)}");
    }

    [Fact]
    public void TheExamples_DependOnTheLibraryAlone()
    {
        var references = ExamplesAssembly.Instance.GetReferencedAssemblies().Select(name => name.Name ?? string.Empty).ToArray();
        Assert.Contains("OmniEurope.Blazor", references);
        Assert.DoesNotContain(references, name => name.StartsWith("OmniEurope.Blazor.Showcase", StringComparison.Ordinal));

        var project = File.ReadAllText(Path.Combine(ExamplesFolder(), "OmniEurope.Blazor.Showcase.Examples.csproj"));
        var projectReferences = ProjectReference().Matches(project).Select(match => match.Groups["path"].Value).ToArray();
        Assert.Equal(["../../src/OmniEurope.Blazor/OmniEurope.Blazor.csproj"], projectReferences);
        Assert.DoesNotContain("PackageReference", project, StringComparison.Ordinal);

        var leaning = ExampleSource.EmbeddedPaths
            .Where(path => ExampleSource.Read(path) is { } source
                && (source.Contains("IStringLocalizer", StringComparison.Ordinal)
                    || source.Replace("OmniEurope.Blazor.Showcase.Examples", string.Empty, StringComparison.Ordinal)
                        .Contains("OmniEurope.Blazor.Showcase", StringComparison.Ordinal)))
            .ToArray();
        Assert.True(leaning.Length == 0, $"Examples using a localizer or a showcase type: {string.Join(", ", leaning)}");
    }

    [Fact]
    public void EveryPageText_ExistsInEveryLanguage()
    {
        foreach (var culture in Cultures)
        {
            ShowcaseLanguageTests.WithUiCulture(culture, () =>
            {
                foreach (var family in GalleryCatalog.Families)
                {
                    var strings = Services.GetRequiredService<IStringLocalizerFactory>().Create(family.Strings);
                    foreach (var page in family.Pages)
                    {
                        Assert.False(strings[page.TitleKey].ResourceNotFound, $"{culture}: {page.TitleKey} missing.");
                        Assert.False(strings[page.DescriptionKey].ResourceNotFound, $"{culture}: {page.DescriptionKey} missing.");
                    }
                }
            });
        }
    }

    [Theory]
    [MemberData(nameof(PageViews))]
    public void EveryPage_RendersEachViewInEachLanguage(string slug, string culture, string view)
    {
        var page = GalleryCatalog.Resolve(slug);
        var component = PageComponent(page)!;

        ShowcaseLanguageTests.WithUiCulture(culture, () =>
        {
            Services.GetRequiredService<NavigationManager>().NavigateTo($"{page.Href}?view={view}");

            var rendered = Render(builder =>
            {
                builder.OpenComponent(0, component);
                builder.CloseComponent();
            });

            Assert.False(string.IsNullOrWhiteSpace(rendered.Markup), $"{slug} rendered nothing.");
            Assert.NotEmpty(rendered.FindAll(".omni-page-header"));
            Assert.DoesNotContain("style=", rendered.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<style", rendered.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("javascript:", rendered.Markup, StringComparison.OrdinalIgnoreCase);
            // A C# enum literal reaching the browser as an attribute value means it fell through to the
            // unmatched-attribute bag. The Code view is left out: it shows sources as text, and their
            // literals are what it is there to show.
            var leaked = LeakedEnumLiteral().Match(rendered.Markup);
            Assert.False(view != "code" && leaked.Success, $"{slug} passes {leaked.Value} to a component that has no such parameter.");

            switch (view)
            {
                case "mobile":
                    Assert.Single(rendered.FindAll(".gallery-phone"));
                    break;
                case "code":
                    Assert.Empty(rendered.FindAll(".gallery-page__body"));
                    Assert.Single(rendered.FindAll(".omni-code-viewer__code"));
                    Assert.Empty(rendered.FindAll(".omni-alert"));
                    break;
                default:
                    Assert.Single(rendered.FindAll(".gallery-page__body"));
                    Assert.Empty(rendered.FindAll(".gallery-phone"));
                    break;
            }
        });
    }

    /// <summary>The routable page of the showcase whose address is the page's.</summary>
    private static Type? PageComponent(GalleryPage page) =>
        typeof(OmniEurope.Blazor.Showcase.Gallery.GalleryCatalog).Assembly.GetTypes()
            .FirstOrDefault(type => typeof(IComponent).IsAssignableFrom(type)
                && type.GetCustomAttributes<RouteAttribute>().Any(route => route.Template == "/" + page.Href));

    private static string ExamplesFolder() =>
        Path.Combine(ShippedLookTests.RepositoryRoot(), "site", "OmniEurope.Blazor.Showcase.Examples");

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex Slug();

    [GeneratedRegex("<ProjectReference Include=\"(?<path>[^\"]+)\"")]
    private static partial Regex ProjectReference();

    [GeneratedRegex("[a-z-]+=\"Omni[A-Za-z]+\\.[A-Za-z]+\"")]
    private static partial Regex LeakedEnumLiteral();
}
