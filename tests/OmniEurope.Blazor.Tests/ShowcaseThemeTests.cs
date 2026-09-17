using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;
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

    /// <summary>
    /// The shape tokens of buttons, cards and headings are typography, elevation and shape, not
    /// colours: the editor must file them where a visitor looks for them.
    /// </summary>
    [Theory]
    [InlineData("--omni-button-shadow", ThemeTokenGroup.Elevation)]
    [InlineData("--omni-card-shadow", ThemeTokenGroup.Elevation)]
    [InlineData("--omni-button-font-weight", ThemeTokenGroup.Typography)]
    [InlineData("--omni-button-text-transform", ThemeTokenGroup.Typography)]
    [InlineData("--omni-button-letter-spacing", ThemeTokenGroup.Typography)]
    [InlineData("--omni-heading-font-family", ThemeTokenGroup.Typography)]
    [InlineData("--omni-heading-text-transform", ThemeTokenGroup.Typography)]
    [InlineData("--omni-button-border-width", ThemeTokenGroup.Shape)]
    [InlineData("--omni-button-border-color", ThemeTokenGroup.Shape)]
    [InlineData("--omni-color-accent", ThemeTokenGroup.Color)]
    [InlineData("--omni-font-family", ThemeTokenGroup.Typography)]
    public void TokenReader_FilesTheShapeTokensUnderTheirFamily(string name, ThemeTokenGroup expected)
    {
        var token = Assert.Single(ThemeTokenReader.Parse($":root {{ {name}: x; }}"));
        Assert.Equal(expected, token.Group);
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
        foreach (var preset in OmniThemePresets.All)
        {
            foreach (var mode in new[] { OmniAppearance.Light, OmniAppearance.Dark })
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
        foreach (var preset in OmniThemePresets.All)
        {
            foreach (var mode in new[] { OmniAppearance.Light, OmniAppearance.Dark })
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
        foreach (var preset in OmniThemePresets.All)
        {
            foreach (var mode in new[] { OmniAppearance.Light, OmniAppearance.Dark })
            {
                var tokens = preset.For(mode);
                foreach (var (fill, over) in new[]
                         {
                             ("--omni-color-accent", "--omni-color-on-accent"),
                             ("--omni-color-accent-strong", "--omni-color-on-accent"),
                             ("--omni-color-danger", "--omni-color-on-danger"),
                             ("--omni-color-success", "--omni-color-on-success"),
                             ("--omni-color-warning", "--omni-color-on-warning"),
                             ("--omni-color-inverse-surface", "--omni-color-on-inverse")
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

    /// <summary>
    /// The stylesheet writes text in the severity colours (badges, outlined alerts, validation
    /// messages), in the strong accent (links, tabs, ghost buttons, the accent badge) and in the muted
    /// colour on the muted surface: each of those pairs must read as body text does.
    /// </summary>
    [Fact]
    public void EveryPalette_KeepsEveryTextTheStylesheetWritesReadable()
    {
        (string Text, string Background)[] pairs =
        [
            ("--omni-color-text-muted", "--omni-color-surface"),
            ("--omni-color-text-muted", "--omni-color-surface-muted"),
            ("--omni-color-accent-strong", "--omni-color-surface"),
            ("--omni-color-accent-strong", "--omni-color-accent-subtle"),
            ("--omni-color-success", "--omni-color-surface"),
            ("--omni-color-success", "--omni-color-success-subtle"),
            ("--omni-color-warning", "--omni-color-surface"),
            ("--omni-color-warning", "--omni-color-warning-subtle"),
            ("--omni-color-danger", "--omni-color-surface"),
            ("--omni-color-danger", "--omni-color-danger-subtle")
        ];

        foreach (var preset in OmniThemePresets.All)
        {
            foreach (var mode in new[] { OmniAppearance.Light, OmniAppearance.Dark })
            {
                var tokens = preset.For(mode);
                foreach (var (text, background) in pairs)
                {
                    var contrast = ThemeColor.Contrast(tokens[text], tokens[background]);
                    Assert.True(
                        contrast >= 4.5,
                        $"{preset.Name} in {mode}: {text} on {background} is {contrast:F2}, below the 4.5 minimum.");
                }
            }
        }
    }

    /// <summary>
    /// A theme may tint its borders or draw them in its text colour, through a token reference or a
    /// <c>color-mix</c>. Resolved, they must stay at least as visible as the derived border of the
    /// palette generator, which mixes a third of the text into the surface.
    /// </summary>
    [Fact]
    public void EveryPalette_KeepsItsBordersVisible()
    {
        foreach (var preset in OmniThemePresets.All)
        {
            foreach (var mode in new[] { OmniAppearance.Light, OmniAppearance.Dark })
            {
                var tokens = preset.For(mode);
                var border = Resolve(tokens, tokens["--omni-color-border"]);
                var contrast = ThemeColor.Contrast(border, tokens["--omni-color-surface"]);
                Assert.True(
                    contrast >= 1.7,
                    $"{preset.Name} in {mode}: the border resolves to {border}, {contrast:F2} against the surface, below the 1.7 minimum.");
            }
        }
    }

    /// <summary>
    /// The pairs above only protect what the stylesheet actually draws: every filled surface must
    /// take the text colour the generator picked for that very fill, never the accent's.
    /// </summary>
    [Fact]
    public void FilledSurfaces_UseTheTextColourPickedForThem()
    {
        var css = File.ReadAllText(Path.Combine(Root, "src", "OmniEurope.Blazor", "wwwroot", "omnieurope.blazor.css"));

        Assert.Matches(@"\.omni-button--success \{[^}]*color: var\(--omni-color-on-success\)", css);
        Assert.Matches(@"\.omni-button--warning \{[^}]*color: var\(--omni-color-on-warning\)", css);
        Assert.Matches(@"\.omni-button--danger \{[^}]*color: var\(--omni-color-on-danger\)", css);
        Assert.Matches(@"\.omni-button--ghost \{[^}]*color: var\(--omni-color-accent-strong\)", css);
        Assert.Matches(@"\.omni-alert--success \{[^}]*--omni-alert-on: var\(--omni-color-on-success\)", css);
        Assert.Matches(@"\.omni-alert--warning \{[^}]*--omni-alert-on: var\(--omni-color-on-warning\)", css);
        Assert.Matches(@"\.omni-alert--danger \{[^}]*--omni-alert-on: var\(--omni-color-on-danger\)", css);
        Assert.Matches(@"\.omni-alert--filled \{[^}]*color: var\(--omni-alert-on\)", css);
        Assert.Matches(@"\.omni-tooltip__content \{[^}]*color: var\(--omni-color-on-inverse\)", css);
    }

    /// <summary>
    /// Twenty themes that differ only by their colours would be one theme twenty times: every pair
    /// must also differ in how things are drawn.
    /// </summary>
    [Fact]
    public void EveryTheme_DrawsThingsDifferentlyFromEveryOther()
    {
        string[] shapeTokens =
        [
            "--omni-radius", "--omni-button-radius", "--omni-card-radius", "--omni-border-width",
            "--omni-button-shadow", "--omni-card-shadow", "--omni-font-family", "--omni-button-text-transform",
            "--omni-card-border-width", "--omni-heading-font-family"
        ];
        var shapes = OmniThemePresets.All.ToDictionary(
            preset => preset.Name,
            preset => string.Join("|", shapeTokens.Select(token => preset.Light.TryGetValue(token, out var value) ? value : "default")));

        Assert.Equal(20, shapes.Count);
        Assert.Equal(shapes.Count, shapes.Values.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void PaletteNames_AreUnique()
    {
        var names = OmniThemePresets.All.Select(preset => preset.Name).ToArray();
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// Resolves the two forms a theme uses for a colour it derives: a token reference and a
    /// <c>color-mix</c> of two of them in sRGB. Anything else fails the test rather than being
    /// guessed, so a new form has to be taught here before it can ship.
    /// </summary>
    private static string Resolve(IReadOnlyDictionary<string, string> tokens, string value)
    {
        if (Regex.IsMatch(value, "^#[0-9a-f]{6}$", RegexOptions.IgnoreCase))
        {
            return value;
        }

        var reference = Regex.Match(value, @"^var\((?<name>--omni-[a-z0-9-]+)\)$");
        if (reference.Success)
        {
            return Resolve(tokens, tokens[reference.Groups["name"].Value]);
        }

        var mix = Regex.Match(value, @"^color-mix\(in srgb, (?<first>var\(--omni-[a-z0-9-]+\)) (?<share>\d+)%, (?<second>var\(--omni-[a-z0-9-]+\))\)$");
        Assert.True(mix.Success, $"Unsupported colour form: {value}");
        var share = int.Parse(mix.Groups["share"].Value, System.Globalization.CultureInfo.InvariantCulture) / 100d;
        return ThemeColor.Mix(Resolve(tokens, mix.Groups["first"].Value), Resolve(tokens, mix.Groups["second"].Value), share);
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
