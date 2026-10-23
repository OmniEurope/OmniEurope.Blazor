using System.Globalization;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The sixteen ANSI colour tokens (<c>--omni-ansi-0</c> to <c>--omni-ansi-15</c>) on every token set a
/// consumer can obtain (every theme with any palette, in light and in dark, the same sets as
/// <see cref="ThemeContrastMatrixTests"/>): each reads at 4.5:1 on the log surface, a background colour
/// keeps every foreground readable, and the sixteen stay apart.
/// </summary>
public sealed class AnsiThemeContrastTests
{
    private const double Text = 4.5;

    private static IReadOnlyDictionary<string, string> TokensOf(string themeName, string paletteName, OmniAppearance mode) =>
        OmniThemePresets.All.Single(entry => entry.Name == themeName)
            .With(OmniThemePalettes.All.Single(entry => entry.Name == paletteName))
            .For(mode);

    private static string[] AnsiColors(IReadOnlyDictionary<string, string> tokens) =>
        [.. Enumerable.Range(0, 16).Select(index => ShowcaseThemeTests.Resolve(tokens, tokens[$"--omni-ansi-{index}"]))];

    /// <summary>
    /// The grounds a log line is drawn on, as the stylesheet resolves <c>--omni-log-viewer-surface</c>:
    /// the grid background, else the card background, else the surface; and, for a theme that paints a
    /// colour field behind the page, that surface laid over each stop of the field.
    /// </summary>
    private static IReadOnlyList<(string Where, string Ground)> LogSurfaces(IReadOnlyDictionary<string, string> tokens, bool darkOnlyInLight)
    {
        var declared = tokens.TryGetValue("--omni-grid-background", out var grid) ? grid
            : tokens.TryGetValue("--omni-card-background", out var card) ? card
            : tokens["--omni-color-surface"];
        var surfaces = new List<(string, string)> { ("the log surface", ShowcaseThemeTests.Resolve(tokens, declared)) };
        if (darkOnlyInLight || !tokens.TryGetValue("--omni-backdrop", out var backdrop) || backdrop == "none")
        {
            return surfaces;
        }

        foreach (var stop in tokens.Keys.Where(key => key.StartsWith("--omni-backdrop-", StringComparison.Ordinal)).Order(StringComparer.Ordinal))
        {
            surfaces.Add(($"the log surface over {stop}", ShowcaseThemeTests.ResolveOver(tokens, declared, ShowcaseThemeTests.Resolve(tokens, tokens[stop]))));
        }

        return surfaces;
    }

    private static bool DarkOnlyInLight(string themeName, OmniAppearance mode) =>
        mode == OmniAppearance.Light && OmniThemePresets.All.Single(entry => entry.Name == themeName).DarkOnly;

    [Theory]
    [MemberData(nameof(ThemeContrastMatrixTests.Sets), MemberType = typeof(ThemeContrastMatrixTests))]
    public void Every_ansi_colour_reads_on_the_log_surface(string themeName, string paletteName, OmniAppearance mode)
    {
        var tokens = TokensOf(themeName, paletteName, mode);
        var colors = AnsiColors(tokens);

        var failures = new List<string>();
        foreach (var (where, ground) in LogSurfaces(tokens, DarkOnlyInLight(themeName, mode)))
        {
            for (var index = 0; index < colors.Length; index++)
            {
                var contrast = ThemeColor.Contrast(colors[index], ground);
                if (contrast < Text)
                {
                    failures.Add(string.Create(CultureInfo.InvariantCulture, $"--omni-ansi-{index} ({colors[index]}) on {where} ({ground}): {contrast:F2}, below {Text:F1}"));
                }
            }
        }

        ThemeContrastMatrixTests.AssertOrAccept(themeName, paletteName, mode, failures);
    }

    /// <summary>
    /// The rule for a background colour: <c>.omni-ansi-bg-N</c> draws <see cref="AnsiTokenBuilder.BackgroundShare"/>
    /// of the colour over the log surface. The default text and each of the sixteen foregrounds stay at 4.5:1
    /// on every one of the sixteen backgrounds, and on the same background inside a warning or an error row.
    /// </summary>
    [Theory]
    [MemberData(nameof(ThemeContrastMatrixTests.Sets), MemberType = typeof(ThemeContrastMatrixTests))]
    public void A_background_colour_keeps_every_foreground_readable(string themeName, string paletteName, OmniAppearance mode)
    {
        var tokens = TokensOf(themeName, paletteName, mode);
        var colors = AnsiColors(tokens);
        var defaultText = ShowcaseThemeTests.Resolve(tokens, tokens["--omni-color-text"]);

        var failures = new List<string>();
        foreach (var (where, surface) in LogSurfaces(tokens, DarkOnlyInLight(themeName, mode)))
        {
            for (var background = 0; background < colors.Length; background++)
            {
                var ground = ThemeColor.Mix(colors[background], surface, AnsiTokenBuilder.BackgroundShare);
                var foregrounds = colors.Select((color, index) => ($"--omni-ansi-{index}", color)).Append(("--omni-color-text", defaultText));
                foreach (var (name, color) in foregrounds)
                {
                    var contrast = ThemeColor.Contrast(color, ground);
                    if (contrast < Text)
                    {
                        failures.Add(string.Create(CultureInfo.InvariantCulture, $"{name} ({color}) on --omni-ansi-{background} background ({ground}) over {where}: {contrast:F2}, below {Text:F1}"));
                    }
                }
            }
        }

        ThemeContrastMatrixTests.AssertOrAccept(themeName, paletteName, mode, failures);
    }

    /// <summary>
    /// In a warning or an error row the line takes the tint of its severity: a colour drawn inside it reads
    /// there as on the surface (the row tints are among the grounds the generator holds the tokens on).
    /// </summary>
    [Theory]
    [MemberData(nameof(ThemeContrastMatrixTests.Sets), MemberType = typeof(ThemeContrastMatrixTests))]
    public void Every_ansi_colour_reads_in_a_warning_or_error_row(string themeName, string paletteName, OmniAppearance mode)
    {
        var tokens = TokensOf(themeName, paletteName, mode);
        var colors = AnsiColors(tokens);

        var failures = new List<string>();
        foreach (var tint in new[] { "--omni-color-warning-subtle", "--omni-color-danger-subtle" })
        {
            var row = ShowcaseThemeTests.Resolve(tokens, tokens[tint]);
            for (var index = 0; index < colors.Length; index++)
            {
                var contrast = ThemeColor.Contrast(colors[index], row);
                if (contrast < Text)
                {
                    failures.Add(string.Create(CultureInfo.InvariantCulture, $"--omni-ansi-{index} ({colors[index]}) on {tint} ({row}): {contrast:F2}, below {Text:F1}"));
                }
            }
        }

        ThemeContrastMatrixTests.AssertOrAccept(themeName, paletteName, mode, failures);
    }

    /// <summary>The sixteen are told apart, and a bright shade is lighter than its normal one, in every set.</summary>
    [Theory]
    [MemberData(nameof(ThemeContrastMatrixTests.Sets), MemberType = typeof(ThemeContrastMatrixTests))]
    public void The_sixteen_colours_are_distinct_and_each_bright_hue_is_lighter_than_its_normal_one(string themeName, string paletteName, OmniAppearance mode)
    {
        var colors = AnsiColors(TokensOf(themeName, paletteName, mode));

        Assert.Equal(16, colors.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        for (var hue = 1; hue <= 6; hue++)
        {
            Assert.True(
                ThemeColor.Luminance(colors[hue + 8]) > ThemeColor.Luminance(colors[hue]),
                $"--omni-ansi-{hue + 8} ({colors[hue + 8]}) is not lighter than --omni-ansi-{hue} ({colors[hue]}) in {themeName} + {paletteName} in {mode}.");
        }
    }

    /// <summary>Light and dark mode carry different values for every colour, in every theme and palette.</summary>
    [Theory]
    [MemberData(nameof(ThemeCatalogPairs))]
    public void Light_and_dark_modes_have_their_own_value_for_each_colour(string themeName, string paletteName)
    {
        var light = AnsiColors(TokensOf(themeName, paletteName, OmniAppearance.Light));
        var dark = AnsiColors(TokensOf(themeName, paletteName, OmniAppearance.Dark));

        for (var index = 0; index < 16; index++)
        {
            Assert.NotEqual(light[index], dark[index], StringComparer.OrdinalIgnoreCase);
        }
    }

    public static TheoryData<string, string> ThemeCatalogPairs()
    {
        var pairs = new TheoryData<string, string>();
        foreach (var theme in OmniThemePresets.All)
        {
            foreach (var palette in OmniThemePalettes.All)
            {
                pairs.Add(theme.Name, palette.Name);
            }
        }

        return pairs;
    }

    /// <summary>The shipped look, written out: the values the stylesheet carries for the default set.</summary>
    [Fact]
    public void The_shipped_look_has_sixteen_tokens_per_mode()
    {
        var shipped = OmniThemePresets.All[0];
        foreach (var mode in new[] { OmniAppearance.Light, OmniAppearance.Dark })
        {
            var tokens = shipped.For(mode);
            Assert.Equal(16, tokens.Keys.Count(key => key.StartsWith("--omni-ansi-", StringComparison.Ordinal)));
        }
    }

    /// <summary>
    /// The stylesheet draws a colour from its token (<c>.omni-ansi-fg-N</c>) and a background as the share
    /// the generator held the foregrounds against (<c>.omni-ansi-bg-N</c>): a change of one without the
    /// other would break the ratio the tests above prove.
    /// </summary>
    [Fact]
    public void The_stylesheet_draws_the_sixteen_colours_from_their_tokens_at_the_share_the_tokens_were_held_for()
    {
        var share = string.Create(CultureInfo.InvariantCulture, $"{AnsiTokenBuilder.BackgroundShare * 100:0}%");
        for (var index = 0; index < 16; index++)
        {
            Assert.Equal($"var(--omni-ansi-{index})", ShippedLookTests.Value(ShippedLookTests.Body($".omni-ansi-fg-{index}"), "color"));
            Assert.Equal(
                $"color-mix(in srgb, var(--omni-ansi-{index}) {share}, transparent)",
                ShippedLookTests.Value(ShippedLookTests.Body($".omni-ansi-bg-{index}"), "background"));
        }

        Assert.Equal("var(--omni-color-text-muted)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-ansi-dim"), "color"));
    }
}
