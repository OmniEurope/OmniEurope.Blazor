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
    /// recette R-347: the solid neutral badge (a grey held between the ratio of its ink and 3:1 against the
    /// surface, <c>--omni-badge-solid-fill-neutral</c>) stands out as a component from the grid and card
    /// frames it sits on, which a theme may tint away from the page (<c>--omni-card-background</c>, the
    /// grid's <c>--omni-grid-frame</c>, else the surface) and from the layer fill. Its ink on it is held at
    /// 4.5 by <see cref="Solid_badges_share_one_white_ink_and_read_at_the_text_ratio"/>.
    /// </summary>
    [Theory]
    [MemberData(nameof(Sets))]
    public void Solid_neutral_badge_stands_out_from_grid_and_card_frames(string themeName, string paletteName, OmniAppearance mode)
    {
        var theme = OmniThemePresets.All.Single(entry => entry.Name == themeName);
        var palette = OmniThemePalettes.All.Single(entry => entry.Name == paletteName);
        var tokens = theme.With(palette).For(mode);
        var fill = ThemeTokenResolution.Resolve(tokens, tokens["--omni-badge-solid-fill-neutral"]);

        var failures = new List<string>();
        foreach (var frame in new[] { "--omni-card-background", "--omni-layer-fill" })
        {
            var value = ThemeTokenResolution.Resolve(tokens, tokens.TryGetValue(frame, out var own) ? own : tokens["--omni-color-surface"]);
            var contrast = ThemeColor.Contrast(fill, value);
            if (contrast < Component)
            {
                failures.Add(string.Create(CultureInfo.InvariantCulture, $"badge fill {fill} on {frame} ({value}): {contrast:F2}, below {Component:F1}"));
            }
        }

        AssertOrAccept(themeName, paletteName, mode, failures);
    }

    /// <summary>The ink of every solid badge, in both modes (owner decision of 2026-10-23, after one ink per mode in recette R-22).</summary>
    internal const string SolidBadgeInk = "#ffffff";

    /// <summary>
    /// The sets in which a solid badge of a tone may fall short of the text ratio or of its single ink.
    /// Empty: the badge tokens are derived from each palette against the white ink, so every
    /// tone of every set holds both. A set added here is a deliberate edit, written with its reason, and
    /// its shortfall is written to the test output.
    /// </summary>
    private static readonly IReadOnlyDictionary<(string Theme, string Palette, OmniAppearance Mode, OmniTone Tone), string> SolidBadgeExceptions =
        new Dictionary<(string, string, OmniAppearance, OmniTone), string>();

    /// <summary>
    /// The one place where the neutral solid badge cannot stand out from the card at 3:1 (WCAG 1.4.11, a
    /// non-text component) and keep its white ink at 4.5:1 at the same time: in dark mode the grey must
    /// be no lighter than a luminance of 0.183 to carry white lettering, while the translucent card Trou
    /// noir lays over its black disk is darker still, so the two ratios cap out between 2.5:1 and 2.9:1 there. The
    /// owner kept the single white ink (recette R-22); the lettering is held at 4.5:1 on every set by
    /// <see cref="Solid_badges_share_one_white_ink_and_read_at_the_text_ratio"/>, and only the frame
    /// of the neutral badge against that card is accepted short, and written to the test output.
    /// </summary>
    private static readonly IReadOnlyDictionary<(string Theme, OmniAppearance Mode), string> NeutralBadgeBackdropExceptions =
        new Dictionary<(string, OmniAppearance), string>
        {
            [("Trou noir", OmniAppearance.Dark)] = "white ink caps the grey below the 3:1 a black-disk card asks",
        };

    /// <summary>
    /// Every solid badge carries the same white ink in both modes (owner decision of 2026-10-23), whatever
    /// its tone, and that ink reads on the fill of its tone at 4.5. The fill and the
    /// ink are read from the declarations of the stylesheet (<c>.omni-badge--solid</c> and its tone rules),
    /// then resolved in the token set, so a stylesheet that paints one tone with another ink fails here.
    /// </summary>
    [Theory]
    [MemberData(nameof(Sets))]
    public void Solid_badges_share_one_white_ink_and_read_at_the_text_ratio(string themeName, string paletteName, OmniAppearance mode)
    {
        var theme = OmniThemePresets.All.Single(entry => entry.Name == themeName);
        var palette = OmniThemePalettes.All.Single(entry => entry.Name == paletteName);
        var tokens = theme.With(palette).For(mode);
        const string wanted = SolidBadgeInk;

        var failures = new List<string>();
        foreach (var tone in Enum.GetValues<OmniTone>())
        {
            var (fillReference, inkReference) = SolidBadgeDeclarations(tone);
            var fill = ThemeTokenResolution.Resolve(tokens, fillReference);
            var ink = ThemeTokenResolution.Resolve(tokens, inkReference);
            var contrast = ThemeColor.Contrast(ink, fill);
            var problems = new List<string>();
            if (!string.Equals(ink, wanted, StringComparison.OrdinalIgnoreCase))
            {
                problems.Add($"ink {ink} is not the badge ink {wanted}");
            }

            if (contrast < Text)
            {
                problems.Add(string.Create(CultureInfo.InvariantCulture, $"{contrast:F2} is below {Text:F1}"));
            }

            if (problems.Count == 0)
            {
                continue;
            }

            var line = $"solid {tone} badge: ink {inkReference} ({ink}) on fill {fillReference} ({fill}): {string.Join(", ", problems)}";
            if (SolidBadgeExceptions.TryGetValue((themeName, paletteName, mode, tone), out var reason))
            {
                TestContext.Current.TestOutputHelper?.WriteLine($"Accepted solid badge exception ({reason}): {themeName} + {paletteName} in {mode}: {line}");
                continue;
            }

            failures.Add(line);
        }

        AssertOrAccept(themeName, paletteName, mode, failures);
    }

    /// <summary>The fill and the ink the stylesheet gives the solid badge of a tone: its own rule over the base rule.</summary>
    private static (string Fill, string Ink) SolidBadgeDeclarations(OmniTone tone)
    {
        var baseBody = ShippedLookTests.Body(".omni-badge--solid");
        var own = $".omni-badge--solid.omni-badge--{tone.ToString().ToLowerInvariant()}";
        var hasOwn = ShippedLookTests.Css.Contains(own + " {", StringComparison.Ordinal);
        var body = hasOwn ? ShippedLookTests.Body(own) : baseBody;
        return (ShippedLookTests.Value(body, "background"), ShippedLookTests.Value(body, "color"));
    }

    /// <summary>The ink of a solid badge that asks for black letters (<c>OmniBadgeInk.Black</c>), in both modes.</summary>
    internal const string SolidBadgeBlackInk = "#000000";

    /// <summary>
    /// A solid badge that asks for black letters (<c>OmniBadgeInk.Black</c>, owner request of 2026-10-23)
    /// writes black in both modes, whatever its tone, on the fill of its tone lightened for black
    /// (<c>--omni-badge-solid-black-fill-X</c>), and reads there at 4.5. Fill and ink are read from the
    /// <c>.omni-badge--ink-black</c> rules of the stylesheet, then resolved in the token set. No exception.
    /// </summary>
    [Theory]
    [MemberData(nameof(Sets))]
    public void Solid_badges_in_black_ink_read_at_the_text_ratio(string themeName, string paletteName, OmniAppearance mode)
    {
        var theme = OmniThemePresets.All.Single(entry => entry.Name == themeName);
        var palette = OmniThemePalettes.All.Single(entry => entry.Name == paletteName);
        var tokens = theme.With(palette).For(mode);
        var baseBody = ShippedLookTests.Body(".omni-badge--solid.omni-badge--ink-black");
        var ink = ThemeTokenResolution.Resolve(tokens, ShippedLookTests.Value(baseBody, "color"));

        var failures = new List<string>();
        foreach (var tone in Enum.GetValues<OmniTone>())
        {
            var own = $".omni-badge--solid.omni-badge--ink-black.omni-badge--{tone.ToString().ToLowerInvariant()}";
            var body = ShippedLookTests.Css.Contains(own + " {", StringComparison.Ordinal) ? ShippedLookTests.Body(own) : baseBody;
            var fillReference = ShippedLookTests.Value(body, "background");
            var fill = ThemeTokenResolution.Resolve(tokens, fillReference);
            var contrast = ThemeColor.Contrast(ink, fill);
            if (ThemeColor.Parse(ink) != ThemeColor.Parse(SolidBadgeBlackInk))
            {
                failures.Add($"black {tone} badge: ink {ink} is not {SolidBadgeBlackInk}");
            }

            if (contrast < Text)
            {
                failures.Add(string.Create(CultureInfo.InvariantCulture, $"black {tone} badge: ink {ink} on fill {fillReference} ({fill}): {contrast:F2}, below {Text:F1}"));
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
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
        var frame = ThemeTokenResolution.Resolve(tokens, tokens.TryGetValue("--omni-grid-background", out var grid) ? grid
            : tokens.TryGetValue("--omni-card-background", out var card) ? card
            : tokens["--omni-color-surface"]);
        var fill = ThemeColor.Mix(ThemeTokenResolution.Resolve(tokens, tokens["--omni-color-text"]), frame, 0.06);
        var ink = ThemeTokenResolution.Resolve(tokens, tokens["--omni-color-text-muted"]);

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

        AssertBackdropMeasures(themeName, theme.Backdrop.Name, paletteName, mode, theme.With(palette).For(mode));
    }

    /// <summary>The themes that offer backdrops, each with every backdrop of the catalogue it draws a field with.</summary>
    public static TheoryData<string, string> Backdrops()
    {
        var combinations = new TheoryData<string, string>();
        foreach (var theme in OmniThemePresets.All)
        {
            foreach (var backdrop in OmniThemePresets.BackdropsFor(theme).Where(entry => !ReferenceEquals(entry, OmniThemeBackdrops.None)))
            {
                combinations.Add(theme.Name, backdrop.Name);
            }
        }

        return combinations;
    }

    /// <summary>
    /// Every theme drawn over every backdrop it offers (review R1-3, orchestrator decision of 2026-11-12: a
    /// backdrop is a setting, so a visitor may pair any theme with any backdrop and any palette), in both
    /// modes: the same measurement as <see cref="Text_reads_on_every_stop_of_a_theme_backdrop"/>, taken on the
    /// theme laid over the backdrop and then over the palette. A readability shortfall fails the combination
    /// unless the theme, or the theme the backdrop belongs to (Givre field), carries a contrast waiver; the fix
    /// for a failing combination is a lighter field, never a lower threshold.
    /// </summary>
    [Theory]
    [MemberData(nameof(Backdrops))]
    public void Text_reads_on_every_stop_of_every_backdrop_under_every_theme(string themeName, string backdropName)
    {
        var theme = OmniThemePresets.All.Single(entry => entry.Name == themeName);
        var backdrop = OmniThemeBackdrops.All.Single(entry => entry.Name == backdropName);
        var drawn = theme.With(backdrop);
        foreach (var palette in OmniThemePalettes.All)
        {
            foreach (var mode in new[] { OmniAppearance.Light, OmniAppearance.Dark })
            {
                if (theme.DarkOnly && mode == OmniAppearance.Light)
                {
                    continue;
                }

                AssertBackdropMeasures(themeName, backdropName, palette.Name, mode, drawn.With(palette).For(mode));
            }
        }
    }

    /// <summary>
    /// Measures the stops a backdrop paints under a theme and a palette. Readability (the text, the muted text
    /// and the strong accent on each stop and on the card over it) fails the set, unless the theme is waived
    /// (<see cref="AssertOrAccept"/>) or the backdrop belongs to a waived theme (the pastel field of Givre, whose
    /// stops are the waived look itself). The frame of the page (the border floor on each stop, the neutral solid
    /// badge on the card over it) is measured and written to the test output as a shortfall accepted for now,
    /// except for the backdrop of Trou noir under Trou noir, which keeps passing it as it did.
    /// </summary>
    /// <remarks>
    /// Open question to the owner (review R1-3): the borders of the palettes sit at the floor on the page (the
    /// Essentiel palette, white surface, in light: 1.71 against 1.7), so any visible tint of the page drops them
    /// under it; a backdrop that keeps the border floor on every palette cannot be seen. Hence the frame is
    /// reported and not failed until the owner says which of the options applies.
    /// </remarks>
    private static void AssertBackdropMeasures(string themeName, string backdropName, string paletteName, OmniAppearance mode, IReadOnlyDictionary<string, string> tokens)
    {
        var (readability, frame) = BackdropFailures(themeName, mode, tokens);
        var failures = readability.Select(failure => $"[{backdropName}] {failure}").ToList();
        var lent = OmniThemePresets.All
            .FirstOrDefault(entry => OmniThemePresets.DefaultBackdropFor(entry).Name == backdropName && entry.ContrastWaiver is not null);
        if (lent is not null && lent.Name != themeName)
        {
            if (failures.Count > 0)
            {
                TestContext.Current.TestOutputHelper?.WriteLine($"Accepted by the contrast waiver of {lent.Name}, which the backdrop belongs to ({failures.Count}): {themeName} + {paletteName} in {mode}:\n{string.Join('\n', failures)}");
            }
        }
        else
        {
            AssertOrAccept(themeName, paletteName, mode, failures);
        }

        if (frame.Count == 0)
        {
            return;
        }

        var report = $"[{backdropName}] {themeName} + {paletteName} in {mode}:\n{string.Join('\n', frame)}";
        Assert.False(themeName == "Trou noir" && backdropName == "Trou noir", report);
        TestContext.Current.TestOutputHelper?.WriteLine($"Accepted frame shortfall, owner question R1-3 ({frame.Count}): {report}");
    }

    /// <summary>
    /// The shortfalls of the stops a token set backdrop paints (none when it paints none): every stop
    /// declared must be painted by the field and every stop painted must be declared. The readability
    /// shortfalls are the text, the muted text and the strong accent on a stop and on the card over it; the
    /// frame shortfalls are the border floor on a stop and the neutral solid badge against that card.
    /// </summary>
    private static (List<string> Readability, List<string> Frame) BackdropFailures(string themeName, OmniAppearance mode, IReadOnlyDictionary<string, string> tokens)
    {
        var declared = tokens.Keys
            .Where(key => key.StartsWith("--omni-backdrop-", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (!tokens.TryGetValue("--omni-backdrop", out var backdrop) || backdrop == "none")
        {
            Assert.Empty(declared);
            return ([], []);
        }

        var stops = BackdropStop().Matches(backdrop)
            .Select(match => match.Groups["name"].Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(stops);
        Assert.Equal(declared, stops);

        var readability = new List<string>();
        var frame = new List<string>();
        foreach (var stop in stops)
        {
            var ground = ThemeTokenResolution.Resolve(tokens, tokens[stop]);
            // A theme that sets no card fill keeps the shipped one, the layer fill (Styles/02-theme-tokens.css).
            var card = ThemeTokenResolution.ResolveOver(tokens, tokens.GetValueOrDefault("--omni-card-background", "var(--omni-layer-fill)"), ground);
            foreach (var (text, ratio) in new[] { ("--omni-color-text", Text), ("--omni-color-text-muted", Text), ("--omni-color-accent-strong", Text) })
            {
                var ink = ThemeTokenResolution.Resolve(tokens, tokens[text]);
                foreach (var (where, background) in new[] { (stop, ground), ($"card over {stop}", card) })
                {
                    var contrast = ThemeColor.Contrast(ink, background);
                    if (contrast < ratio)
                    {
                        readability.Add(string.Create(CultureInfo.InvariantCulture, $"{text} ({ink}) on {where} ({background}): {contrast:F2}, below {ratio:F1}"));
                    }
                }
            }

            var border = ThemeTokenResolution.Resolve(tokens, tokens["--omni-color-border"]);
            var borderContrast = ThemeColor.Contrast(border, ground);
            if (borderContrast < BorderFloor)
            {
                frame.Add(string.Create(CultureInfo.InvariantCulture, $"--omni-color-border ({border}) on {stop} ({ground}): {borderContrast:F2}, below {BorderFloor:F1}"));
            }

            var badge = ThemeTokenResolution.Resolve(tokens, tokens["--omni-badge-solid-fill-neutral"]);
            var badgeContrast = ThemeColor.Contrast(badge, card);
            if (badgeContrast < Component)
            {
                var line = string.Create(CultureInfo.InvariantCulture, $"badge fill {badge} on card over {stop} ({card}): {badgeContrast:F2}, below {Component:F1}");
                if (NeutralBadgeBackdropExceptions.TryGetValue((themeName, mode), out var reason))
                {
                    TestContext.Current.TestOutputHelper?.WriteLine($"Accepted neutral solid badge exception ({reason}): {themeName} in {mode}: {line}");
                }
                else
                {
                    frame.Add(line);
                }
            }
        }

        return (readability, frame);
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
    internal static void AssertOrAccept(string themeName, string paletteName, OmniAppearance mode, IReadOnlyCollection<string> failures)
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

#if OMNI_SHOWCASE_REFERENCED
    // The showcase is referenced on net11.0 only (OmniEurope.Blazor.Tests.csproj): this check of its audit runs there.
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
#endif

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
        var foregroundValue = ThemeTokenResolution.Resolve(tokens, tokens[foreground]);
        var backgroundValue = ThemeTokenResolution.Resolve(tokens, tokens[background]);
        return (foreground, foregroundValue, background, backgroundValue, ThemeColor.Contrast(foregroundValue, backgroundValue), ratio);
    }
}
