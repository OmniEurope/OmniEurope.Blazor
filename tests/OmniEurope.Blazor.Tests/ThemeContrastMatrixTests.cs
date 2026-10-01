using System.Globalization;
using System.Text.RegularExpressions;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// PLAN-004 lot 8: the static contrast pairs, checked on every token set a consumer can obtain (every
/// theme, each painted with any palette, in light and in dark), not only on each theme's default
/// palette. The pairs start from the 47 of the reference mockup (<c>PAIRS</c> in
/// <c>docs/plans/archive/PLAN-004-maquette-themes.html</c>) and add the ones the plan requires on top of them.
/// </summary>
/// <remarks>
/// A theme's shape may write a colour token as a reference or a <c>color-mix</c> (the borders of Halo,
/// Néon, Papier, Rétro and Octet, and their dark shapes): every value is resolved the way
/// <see cref="ShowcaseThemeTests.EveryPalette_KeepsItsBordersVisible"/> resolves it before being
/// measured, and a form it cannot resolve fails the set.
/// </remarks>
public sealed partial class ThemeContrastMatrixTests
{
    private const double Text = 4.5;
    private const double Component = 3.0;

    /// <summary>
    /// OE's own floor for the border against the page, the one
    /// <see cref="ShowcaseThemeTests.EveryPalette_KeepsItsBordersVisible"/> motivates: a theme's border
    /// must stay at least as visible as the generator's, a third of the text in the surface.
    /// </summary>
    /// <remarks>
    /// Arbitration (PLAN-004 lot 8): WCAG 1.4.11 asks 3.0 for the boundary of a component only when
    /// that boundary is the one thing that identifies it. The OE controls that draw a border are also
    /// told apart by their fill, their label or their focus ring, and a 3.0 hairline on every card,
    /// table cell and separator would weigh on every palette. The owner kept 1.7 on 2026-09-18;
    /// raising it is this one constant.
    /// </remarks>
    private const double BorderFloor = 1.7;

    /// <summary>
    /// The rendering margin of RET-002 n°51, applied where
    /// <see cref="ShowcaseThemeTests.ShippedLightTheme_KeepsRenderingMarginForFilledControls"/> applies
    /// it: the shipped look (theme and palette <c>Essentiel</c>) in light mode, on its filled controls.
    /// </summary>
    private const double RenderingMargin = 5.0;

    private static readonly (string Text, string Background)[] RenderingMarginPairs =
    [
        ("--omni-color-on-accent", "--omni-color-accent"),
        ("--omni-color-on-accent", "--omni-color-accent-strong"),
        ("--omni-color-on-danger", "--omni-color-danger"),
        ("--omni-color-on-success", "--omni-color-success"),
        ("--omni-color-on-warning", "--omni-color-warning"),
    ];

    /// <summary>The pairs every set must hold, foreground first.</summary>
    internal static readonly (string Foreground, string Background, double Ratio)[] Pairs =
    [
        // Body text on every surface a row, a menu or a cell can take.
        ("--omni-color-text", "--omni-color-surface", Text),
        ("--omni-color-text", "--omni-color-surface-muted", Text),
        ("--omni-color-text", "--omni-color-surface-hover", Text),
        ("--omni-color-text", "--omni-color-surface-highlight", Text),
        ("--omni-color-text-muted", "--omni-color-surface", Text),
        ("--omni-color-text-muted", "--omni-color-surface-muted", Text),
        ("--omni-color-text-muted", "--omni-color-surface-hover", Text),

        // The strong accent is text: links, tabs, ghost buttons at rest, hovered and pressed.
        ("--omni-color-accent-strong", "--omni-color-surface", Text),
        ("--omni-color-accent-strong", "--omni-color-accent-subtle", Text),
        ("--omni-color-accent-strong", "--omni-color-surface-muted", Text),
        ("--omni-color-accent-strong", "--omni-color-surface-hover", Text),
        ("--omni-color-accent-strong", "--omni-color-surface-highlight", Text),

        // Text laid over the text-grade colours.
        ("--omni-color-on-accent", "--omni-color-accent", Text),
        ("--omni-color-on-accent", "--omni-color-accent-strong", Text),
        ("--omni-color-on-success", "--omni-color-success", Text),
        ("--omni-color-on-warning", "--omni-color-warning", Text),
        ("--omni-color-on-danger", "--omni-color-danger", Text),
        ("--omni-color-on-info", "--omni-color-info", Text),
        ("--omni-color-on-inverse", "--omni-color-inverse-surface", Text),

        // Severities written as text, on the page and on their own tint.
        ("--omni-color-success", "--omni-color-surface", Text),
        ("--omni-color-success", "--omni-color-success-subtle", Text),
        ("--omni-color-warning", "--omni-color-surface", Text),
        ("--omni-color-warning", "--omni-color-warning-subtle", Text),
        ("--omni-color-danger", "--omni-color-surface", Text),
        ("--omni-color-danger", "--omni-color-danger-subtle", Text),
        ("--omni-color-info", "--omni-color-surface", Text),
        ("--omni-color-info", "--omni-color-info-subtle", Text),

        // Brand fills (T1) and their states (T3).
        .. new[] { "accent", "success", "info", "warning", "danger" }.SelectMany(name => new[]
        {
            ($"--omni-color-on-{name}-fill", $"--omni-color-{name}-fill", Text),
            ($"--omni-color-on-{name}-fill", $"--omni-color-{name}-fill-hover", Text),
            ($"--omni-color-on-{name}-fill", $"--omni-color-{name}-fill-active", Text),
            ($"--omni-color-{name}-fill", "--omni-color-surface", Component),
        }),

        // Buttons, filled badges, notification marks and alerts (T22, T24).
        .. new[] { "info", "success" }.SelectMany(name => new[] { string.Empty, "-hover", "-active" }
            .Select(state => ("--omni-color-on-bright", $"--omni-color-{name}-bright{state}", Text))),
        .. new[] { "warning", "danger" }.SelectMany(name => new[] { string.Empty, "-hover", "-active" }
            .Select(state => ("--omni-color-on-deep", $"--omni-color-{name}-deep{state}", Text))),

        // The secondary button (T10).
        ("--omni-color-text", "--omni-color-neutral-fill", Text),
        ("--omni-color-text", "--omni-color-neutral-fill-hover", Text),
        ("--omni-color-text", "--omni-color-neutral-fill-active", Text),

        ("--omni-color-accent", "--omni-color-surface", Component),
        ("--omni-color-border", "--omni-color-surface", BorderFloor),
    ];

    /// <summary>Every theme with every palette, in both modes.</summary>
    public static TheoryData<string, string, OmniAppearance> Sets()
    {
        var sets = new TheoryData<string, string, OmniAppearance>();
        foreach (var theme in OmniThemePresets.All)
        {
            foreach (var palette in OmniThemePalettes.All)
            {
                sets.Add(theme.Name, palette.Name, OmniAppearance.Light);
                sets.Add(theme.Name, palette.Name, OmniAppearance.Dark);
            }
        }

        return sets;
    }

    [Theory]
    [MemberData(nameof(Sets))]
    public void Every_set_holds_every_pair(string theme, string palette, OmniAppearance mode)
    {
        var failures = Measure(theme, palette, mode)
            .Where(check => check.Contrast < check.Required)
            .Select(check => string.Create(
                CultureInfo.InvariantCulture,
                $"{check.Foreground} ({check.ForegroundValue}) on {check.Background} ({check.BackgroundValue}): {check.Contrast:F2}, below {check.Required:F1}"))
            .ToArray();

        AssertOrAccept(theme, palette, mode, failures);
    }

    /// <summary>
    /// Aetheus recette R-347: the solid neutral badge (muted text as fill, surface as ink) stands out as a
    /// component from the grid and card frames it sits on, which a theme may tint away from the page
    /// (<c>--omni-card-background</c>, the grid's <c>--omni-grid-frame</c>, else the surface) and from the layer
    /// fill. Its ink on it is the muted-text-on-surface pair of <see cref="Pairs"/>.
    /// </summary>
    [Theory]
    [MemberData(nameof(Sets))]
    public void Solid_neutral_badge_stands_out_from_grid_and_card_frames(string themeName, string paletteName, OmniAppearance mode)
    {
        var theme = OmniThemePresets.All.Single(entry => entry.Name == themeName);
        var palette = OmniThemePalettes.All.Single(entry => entry.Name == paletteName);
        var tokens = theme.With(palette).For(mode);
        var fill = ShowcaseThemeTests.Resolve(tokens, tokens["--omni-color-text-muted"]);

        var failures = new List<string>();
        foreach (var frame in new[] { "--omni-card-background", "--omni-layer-fill" })
        {
            var value = ShowcaseThemeTests.Resolve(tokens, tokens.TryGetValue(frame, out var own) ? own : tokens["--omni-color-surface"]);
            var contrast = ThemeColor.Contrast(fill, value);
            if (contrast < Component)
            {
                failures.Add(string.Create(CultureInfo.InvariantCulture, $"badge fill {fill} on {frame} ({value}): {contrast:F2}, below {Component:F1}"));
            }
        }

        AssertOrAccept(themeName, paletteName, mode, failures);
    }

    /// <summary>
    /// The header of a grid writes its titles in muted text on a fill one step darker than the frame
    /// (6 % of the text colour over <c>--omni-grid-frame</c>), which a theme may already tint away from
    /// the surface. The contrast probe found the pair at 4.48 under Trou noir with the Électrique palette
    /// in light mode (2026-09-30), where the palette matrix alone saw nothing.
    /// </summary>
    [Theory]
    [MemberData(nameof(Sets))]
    public void Grid_header_titles_read_on_the_tinted_header_fill(string themeName, string paletteName, OmniAppearance mode)
    {
        var theme = OmniThemePresets.All.Single(entry => entry.Name == themeName);
        var palette = OmniThemePalettes.All.Single(entry => entry.Name == paletteName);
        var tokens = theme.With(palette).For(mode);
        // The frame of the stylesheet: --omni-grid-background, else the card fill, else the surface; a
        // translucent one is read over the opaque surface the sticky header lays behind its cells.
        var frame = ShowcaseThemeTests.Resolve(tokens, tokens.TryGetValue("--omni-grid-background", out var grid) ? grid
            : tokens.TryGetValue("--omni-card-background", out var card) ? card
            : tokens["--omni-color-surface"]);
        var fill = ThemeColor.Mix(ShowcaseThemeTests.Resolve(tokens, tokens["--omni-color-text"]), frame, 0.06);
        var ink = ShowcaseThemeTests.Resolve(tokens, tokens["--omni-color-text-muted"]);

        var contrast = ThemeColor.Contrast(ink, fill);
        var failures = contrast < Text
            ? new[] { string.Create(CultureInfo.InvariantCulture, $"--omni-color-text-muted ({ink}) on the grid header fill ({fill}): {contrast:F2}, below {Text:F1}") }
            : [];

        AssertOrAccept(themeName, paletteName, mode, failures);
    }

    /// <summary>
    /// A theme may paint a colour field behind the page (<c>--omni-backdrop</c>, Givre), gradients
    /// between the stops it declares as <c>--omni-backdrop-*</c> tokens (Givre: <c>-start</c>,
    /// <c>-middle</c>, <c>-end</c> and <c>-glow</c>). The stops are read from the field itself, and
    /// every declared stop must be painted by it, so a stop added to a theme is measured without editing
    /// this test. Every text the page writes directly on the scope must read on each stop as it reads on the
    /// surface, the translucent card laid over each stop too, the border must keep its floor there, and
    /// the solid neutral badge must stand
    /// out from that card. The contrast probe measures the painted gradient itself, stop by stop.
    /// </summary>
    [Theory]
    [MemberData(nameof(Sets))]
    public void Text_reads_on_every_stop_of_a_theme_backdrop(string themeName, string paletteName, OmniAppearance mode)
    {
        var theme = OmniThemePresets.All.Single(entry => entry.Name == themeName);
        var palette = OmniThemePalettes.All.Single(entry => entry.Name == paletteName);
        if (theme.DarkOnly && mode == OmniAppearance.Light)
        {
            // A theme drawn in dark mode only never paints its field on a light page: the scope draws its
            // dark half whatever the mode (OmniThemeScope.EffectiveAppearance, tested on its own).
            return;
        }

        var tokens = theme.With(palette).For(mode);
        var declared = tokens.Keys
            .Where(key => key.StartsWith("--omni-backdrop-", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (!tokens.TryGetValue("--omni-backdrop", out var backdrop) || backdrop == "none")
        {
            Assert.Empty(declared);
            return;
        }

        var stops = BackdropStop().Matches(backdrop)
            .Select(match => match.Groups["name"].Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(stops);
        Assert.Equal(declared, stops);

        var failures = new List<string>();
        foreach (var stop in stops)
        {
            var ground = ShowcaseThemeTests.Resolve(tokens, tokens[stop]);
            var card = ShowcaseThemeTests.ResolveOver(tokens, tokens["--omni-card-background"], ground);
            foreach (var (text, ratio) in new[] { ("--omni-color-text", Text), ("--omni-color-text-muted", Text), ("--omni-color-accent-strong", Text) })
            {
                var ink = ShowcaseThemeTests.Resolve(tokens, tokens[text]);
                foreach (var (where, background) in new[] { (stop, ground), ($"card over {stop}", card) })
                {
                    var contrast = ThemeColor.Contrast(ink, background);
                    if (contrast < ratio)
                    {
                        failures.Add(string.Create(CultureInfo.InvariantCulture, $"{text} ({ink}) on {where} ({background}): {contrast:F2}, below {ratio:F1}"));
                    }
                }
            }

            var border = ShowcaseThemeTests.Resolve(tokens, tokens["--omni-color-border"]);
            var borderContrast = ThemeColor.Contrast(border, ground);
            if (borderContrast < BorderFloor)
            {
                failures.Add(string.Create(CultureInfo.InvariantCulture, $"--omni-color-border ({border}) on {stop} ({ground}): {borderContrast:F2}, below {BorderFloor:F1}"));
            }

            var badge = ShowcaseThemeTests.Resolve(tokens, tokens["--omni-color-text-muted"]);
            var badgeContrast = ThemeColor.Contrast(badge, card);
            if (badgeContrast < Component)
            {
                failures.Add(string.Create(CultureInfo.InvariantCulture, $"badge fill {badge} on card over {stop} ({card}): {badgeContrast:F2}, below {Component:F1}"));
            }
        }

        AssertOrAccept(themeName, paletteName, mode, failures);
    }

    /// <summary>
    /// The themes whose style wins over the contrast thresholds (owner decision of 2026-09-28). The list
    /// is written here on purpose: waiving one more theme is a deliberate edit of this test, never a
    /// side effect, and the shipped look (Essentiel) can never be waived.
    /// </summary>
    [Fact]
    public void Only_the_declared_themes_waive_the_contrast_thresholds_and_each_says_why()
    {
        var waived = OmniThemePresets.All.Where(theme => theme.ContrastWaiver is not null).ToArray();

        Assert.Equal(["Relief", "Givre", "Aplat"], waived.Select(theme => theme.Name));
        Assert.All(waived, theme => Assert.False(string.IsNullOrWhiteSpace(theme.ContrastWaiver)));
        Assert.Null(OmniThemePresets.All[0].ContrastWaiver);
    }

    /// <summary>
    /// A theme without a waiver fails on any shortfall. A waived theme is still measured: its shortfalls
    /// are written to the test output as accepted, so the gap stays visible, and the test passes.
    /// </summary>
    private static void AssertOrAccept(string themeName, string paletteName, OmniAppearance mode, IReadOnlyCollection<string> failures)
    {
        var report = $"{themeName} + {paletteName} in {mode}:\n{string.Join('\n', failures)}";
        if (OmniThemePresets.All.Single(entry => entry.Name == themeName).ContrastWaiver is null)
        {
            Assert.True(failures.Count == 0, report);
            return;
        }

        if (failures.Count > 0)
        {
            TestContext.Current.TestOutputHelper?.WriteLine($"Accepted by contrast waiver ({failures.Count}): {report}");
        }
    }

    /// <summary>A stop the backdrop paints: a <c>var()</c> of an <c>--omni-backdrop-*</c> token.</summary>
    [GeneratedRegex(@"var\((?<name>--omni-backdrop-[a-z0-9-]+)\)")]
    private static partial Regex BackdropStop();

    /// <summary>
    /// The size of the matrix: every theme with every palette in both modes, every pair of each, so
    /// that a set or a pair skipped by the enumeration cannot pass unseen. The catalogue sizes
    /// themselves are open and read from the catalogues.
    /// </summary>
    [Fact]
    public void The_matrix_covers_every_set_and_every_pair_of_each()
    {
        var sets = Sets().Select(row => row.Data).ToArray();
        var checks = sets.Sum(set => Measure(set.Item1, set.Item2, set.Item3).Count);
        var expected = OmniThemePresets.All.Count * OmniThemePalettes.All.Count * 2;

        Assert.Equal(expected, sets.Length);
        Assert.Equal(expected, sets.Distinct().Count());
        Assert.Equal(Pairs.Length, Pairs.Select(pair => (pair.Foreground, pair.Background)).Distinct().Count());
        Assert.Equal((expected * Pairs.Length) + RenderingMarginPairs.Length, checks);
    }

    /// <summary>
    /// The customizer's live audit (<see cref="Showcase.Theming.ContrastAudit"/>) shows a visitor the
    /// pairs of the mockup: each must be one this matrix holds on every set, at the same threshold, or
    /// the page would grade a pair the package never promised, or at another bar than the package's.
    /// </summary>
    [Fact]
    public void The_customizer_audit_shows_only_pairs_this_matrix_holds_at_the_same_threshold()
    {
        var held = Pairs.ToDictionary(pair => (pair.Foreground, pair.Background), pair => pair.Ratio);

        Assert.NotEmpty(Showcase.Theming.ContrastAudit.Pairs);
        var foreign = Showcase.Theming.ContrastAudit.Pairs
            .Where(pair => !held.TryGetValue((pair.Foreground, pair.Background), out var ratio) || ratio != pair.Minimum)
            .Select(pair => string.Create(CultureInfo.InvariantCulture, $"{pair.LabelKey}: {pair.Foreground} on {pair.Background} at {pair.Minimum:F1}"))
            .ToArray();
        Assert.True(foreign.Length == 0, $"Audit pairs absent from the matrix or at another threshold:\n{string.Join('\n', foreign)}");
    }

    private static List<(string Foreground, string ForegroundValue, string Background, string BackgroundValue, double Contrast, double Required)> Measure(
        string themeName, string paletteName, OmniAppearance mode)
    {
        var theme = OmniThemePresets.All.Single(entry => entry.Name == themeName);
        var palette = OmniThemePalettes.All.Single(entry => entry.Name == paletteName);
        var tokens = theme.With(palette).For(mode);
        var checks = new List<(string, string, string, string, double, double)>();

        foreach (var (foreground, background, ratio) in Pairs)
        {
            checks.Add(Check(tokens, foreground, background, ratio));
        }

        if (themeName == "Essentiel" && paletteName == "Essentiel" && mode is OmniAppearance.Light)
        {
            foreach (var (text, background) in RenderingMarginPairs)
            {
                checks.Add(Check(tokens, text, background, RenderingMargin));
            }
        }

        return checks;
    }

    private static (string, string, string, string, double, double) Check(
        IReadOnlyDictionary<string, string> tokens, string foreground, string background, double ratio)
    {
        var foregroundValue = ShowcaseThemeTests.Resolve(tokens, tokens[foreground]);
        var backgroundValue = ShowcaseThemeTests.Resolve(tokens, tokens[background]);
        return (foreground, foregroundValue, background, backgroundValue, ThemeColor.Contrast(foregroundValue, backgroundValue), ratio);
    }
}
