using System.Text.RegularExpressions;
using OmniEurope.Blazor.Showcase.Demos;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Guards the showcase against the two ways it could quietly start lying: a theme editor that no
/// longer exposes what the stylesheet ships, and a gallery that shows code it does not run.
/// </summary>
public sealed class ShowcaseThemeTests
{
    private static readonly string Root = RepositoryRoot();

    [Fact]
    public void TokenReader_FindsEveryCustomPropertyTheStylesheetDeclares()
    {
        var css = File.ReadAllText(Path.Combine(Root, "src", "OmniEurope.Blazor", "wwwroot", "omnieurope.blazor.css"));
        var declared = DeclaredRootTokens(css);
        var parsed = ThemeTokenReader.Parse(css).Select(token => token.Name).ToArray();

        Assert.NotEmpty(declared);
        Assert.Equal(declared, parsed);
    }

    [Fact]
    public void EveryDemo_ShipsTheSourceItExecutes()
    {
        Assert.NotEmpty(DemoCatalog.All);
        foreach (var demo in DemoCatalog.All)
        {
            var source = DemoSource.Read(demo.Component);
            Assert.False(string.IsNullOrWhiteSpace(source), $"No embedded source for {demo.Component.Name}.");
        }
    }

    [Fact]
    public void DemoKeys_AreUniqueAndUrlSafe()
    {
        var keys = DemoCatalog.All.Select(demo => demo.Key).ToArray();
        Assert.Equal(keys.Length, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.All(keys, key => Assert.Matches("^[a-z0-9-]+$", key));
    }

    [Fact]
    public void EveryPalette_KeepsBodyTextReadableInBothModes()
    {
        foreach (var preset in ThemePresets.All)
        {
            foreach (var mode in new[] { ThemeMode.Light, ThemeMode.Dark })
            {
                var tokens = preset.For(mode);
                var contrast = ThemeColor.Contrast(tokens["--omni-color-text"], tokens["--omni-color-surface"]);
                Assert.True(
                    contrast >= 4.5,
                    $"{preset.Name} in {mode}: body text contrast is {contrast:F2}, below the 4.5 minimum.");
            }
        }
    }

    [Fact]
    public void EveryPalette_KeepsTheAccentVisibleAgainstItsSurface()
    {
        foreach (var preset in ThemePresets.All)
        {
            foreach (var mode in new[] { ThemeMode.Light, ThemeMode.Dark })
            {
                var tokens = preset.For(mode);
                var contrast = ThemeColor.Contrast(tokens["--omni-color-accent"], tokens["--omni-color-surface"]);
                Assert.True(
                    contrast >= 3.0,
                    $"{preset.Name} in {mode}: accent contrast is {contrast:F2}, below the 3.0 minimum.");
            }
        }
    }

    [Fact]
    public void EveryPalette_KeepsTextReadableOnFilledSurfaces()
    {
        foreach (var preset in ThemePresets.All)
        {
            foreach (var mode in new[] { ThemeMode.Light, ThemeMode.Dark })
            {
                var tokens = preset.For(mode);
                foreach (var (fill, over) in new[]
                         {
                             ("--omni-color-accent", "--omni-color-on-accent"),
                             ("--omni-color-danger", "--omni-color-on-danger"),
                             ("--omni-color-success", "--omni-color-on-success")
                         })
                {
                    var contrast = ThemeColor.Contrast(tokens[fill], tokens[over]);
                    Assert.True(
                        contrast >= 4.5,
                        $"{preset.Name} in {mode}: {over} over {fill} is {contrast:F2}, below the 4.5 minimum.");
                }
            }
        }
    }

    [Fact]
    public void EveryPalette_DeclaresWhichHalfWasDerived()
    {
        Assert.NotEmpty(ThemePresets.All);
        foreach (var preset in ThemePresets.All)
        {
            Assert.NotNull(preset.DerivedMode);
            Assert.Contains("MIT", preset.Origin, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void PaletteNames_AreUnique()
    {
        var names = ThemePresets.All.Select(preset => preset.Name).ToArray();
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    private static string[] DeclaredRootTokens(string css)
    {
        var block = Regex.Match(css, @"^:root\s*\{(?<body>[^}]*)\}", RegexOptions.Multiline);
        Assert.True(block.Success, "The stylesheet no longer opens with a :root block.");
        return [.. Regex.Matches(block.Groups["body"].Value, @"^\s*(?<name>--omni-[a-z0-9-]+)\s*:", RegexOptions.Multiline)
            .Select(match => match.Groups["name"].Value)];
    }

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
