using System.Globalization;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// PLAN-008 lot 8: the static contrast pairs, checked on every one of the 200 token sets a consumer
/// can obtain (10 themes, each painted with any of the 10 palettes, in light and in dark), not only on
/// each theme's default palette. The pairs start from the 47 of the reference mockup (<c>PAIRS</c> in
/// <c>plans/PLAN-008-maquette-themes.html</c>) and add the ones the plan requires on top of them.
/// </summary>
/// <remarks>
/// A theme's shape may write a colour token as a reference or a <c>color-mix</c> (the borders of Halo,
/// Néon, Papier, Rétro and Octet, and their dark shapes): every value is resolved the way
/// <see cref="ShowcaseThemeTests.EveryPalette_KeepsItsBordersVisible"/> resolves it before being
/// measured, and a form it cannot resolve fails the set.
/// </remarks>
public sealed class ThemeContrastMatrixTests
{
    private const double Text = 4.5;
    private const double Component = 3.0;

    /// <summary>
    /// OE's own floor for the border against the page, the one
    /// <see cref="ShowcaseThemeTests.EveryPalette_KeepsItsBordersVisible"/> motivates: a theme's border
    /// must stay at least as visible as the generator's, a third of the text in the surface.
    /// </summary>
    /// <remarks>
    /// Arbitration (PLAN-008 lot 8): WCAG 1.4.11 asks 3.0 for the boundary of a component only when
    /// that boundary is the one thing that identifies it. The OE controls that draw a border are also
    /// told apart by their fill, their label or their focus ring, and a 3.0 hairline on every card,
    /// table cell and separator would weigh on every palette. The owner kept 1.7 on 2026-09-18;
    /// raising it is this one constant.
    /// </remarks>
    private const double BorderFloor = 1.7;

    /// <summary>
    /// The rendering margin of RET-002 n°51, applied where
    /// <see cref="ShowcaseThemeTests.ShippedLightTheme_KeepsRenderingMarginForFilledControls"/> applies
    /// it: the shipped look (theme and palette <c>Défaut</c>) in light mode, on its filled controls.
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

        Assert.True(failures.Length == 0, $"{theme} + {palette} in {mode}:\n{string.Join('\n', failures)}");
    }

    /// <summary>The size of the matrix, so that a shrinking catalogue or pair list cannot pass unseen.</summary>
    [Fact]
    public void The_matrix_covers_200_sets_and_every_pair_of_each()
    {
        var sets = Sets().Select(row => row.Data).ToArray();
        var checks = sets.Sum(set => Measure(set.Item1, set.Item2, set.Item3).Count);

        Assert.Equal(200, sets.Length);
        Assert.Equal(200, sets.Distinct().Count());
        Assert.Equal(Pairs.Length, Pairs.Select(pair => (pair.Foreground, pair.Background)).Distinct().Count());
        Assert.Equal((200 * Pairs.Length) + RenderingMarginPairs.Length, checks);
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

        if (themeName == "Défaut" && paletteName == "Défaut" && mode is OmniAppearance.Light)
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
