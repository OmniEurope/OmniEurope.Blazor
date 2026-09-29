using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Non-regression of the shipped look ported from the PLAN-004 mockup (lot 7): button states, the
/// fills shared by buttons, badges and alerts, letter spacing, progress, busy veil, the layer tokens
/// and the small radius. Each test reads the source stylesheet, or renders the component when the
/// markup is what matters.
/// </summary>
public sealed partial class ShippedLookTests : OmniBunitContext
{
    internal static readonly string Css = Uncommented(File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "omnieurope.blazor.css")));

    [GeneratedRegex(@"(?<selector>[^{}]+)\{(?<body>[^{}]*)\}")]
    private static partial Regex Rule();

    // ---- T3, T9: every variant hovers and presses through its own shades ----

    [Fact]
    public void EveryButtonVariant_NamesItsFillItsHoverItsPressAndItsInk()
    {
        foreach (var variant in Enum.GetNames<OmniButtonVariant>().Select(name => name.ToLowerInvariant()))
        {
            var body = Body($".omni-button--{variant}");
            foreach (var part in new[] { "--omni-button-fill:", "--omni-button-fill-hover:", "--omni-button-fill-active:", "--omni-button-ink:" })
            {
                Assert.Contains(part, body, StringComparison.Ordinal);
            }
        }

        Assert.Contains("background: var(--omni-button-fill-hover)", Body(".omni-button:hover:not(:disabled)"), StringComparison.Ordinal);
        var press = Body(".omni-button:active:not(:disabled)");
        Assert.Contains("background: var(--omni-button-fill-active)", press, StringComparison.Ordinal);
        Assert.Contains("transform: var(--omni-button-press-transform, translateY(1px))", press, StringComparison.Ordinal);
        Assert.Contains("box-shadow: var(--omni-button-press-shadow, var(--omni-button-shadow))", press, StringComparison.Ordinal);
    }

    [Fact]
    public void EverySplitButtonVariant_HasTheFillsOfTheButtonOfTheSameVariant()
    {
        foreach (var variant in Enum.GetValues<OmniButtonVariant>())
        {
            var name = variant.ToString().ToLowerInvariant();
            // Secondary renders without a class: its colours sit on the component root.
            var split = variant == OmniButtonVariant.Secondary ? BodyWith(".omni-split-button", "--omni-button-fill:") : Body($".omni-split-button--{name}");
            var button = Body($".omni-button--{name}");
            foreach (var token in new[] { "--omni-button-fill", "--omni-button-fill-hover", "--omni-button-fill-active", "--omni-button-ink" })
            {
                Assert.Equal(Value(button, token), Value(split, token));
            }
        }

        const string Parts = ".omni-split-button > :is(.omni-split-button__main, .omni-split-button__toggle)";
        Assert.Contains("background: var(--omni-button-fill-hover)", Body(Parts + ":hover:not(:disabled)"), StringComparison.Ordinal);
        Assert.Contains("background: var(--omni-button-fill-active)", Body(Parts + ":active:not(:disabled)"), StringComparison.Ordinal);
        Assert.Contains("transform: var(--omni-button-press-transform, translateY(1px))", Body(Parts + ":active:not(:disabled)"), StringComparison.Ordinal);
    }

    [Fact]
    public void NoHoverRule_PaintsALiteralColourOrMixesInWhite()
    {
        var offenders = Rules()
            .Where(rule => rule.Selector.Contains(":hover", StringComparison.Ordinal))
            .Where(rule => Regex.IsMatch(rule.Body, @"#(?!0000\b)[0-9a-fA-F]{3,8}\b|\brgba?\(|\bhsla?\(|\bwhite\b|\bblack\b"))
            .Select(rule => rule.Selector)
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void ButtonPress_AnimatesTransformAndShadowExceptUnderReducedMotion()
    {
        var transition = Value(Body(".omni-button"), "transition");
        Assert.Contains("transform var(--omni-duration-press)", transition, StringComparison.Ordinal);
        Assert.Contains("box-shadow var(--omni-duration-fast)", transition, StringComparison.Ordinal);
        Assert.Equal("80ms", Value(Body(":root"), "--omni-duration-press"));
        Assert.Equal("120ms", Value(Body(":root"), "--omni-duration-fast"));
        Assert.Matches(@"@media \(prefers-reduced-motion: reduce\) \{\s*\.omni-button \{ animation: none; transition: none; \}", Css);
    }

    // ---- T6, T10: Secondary is a grey fill without border and keeps the theme's relief ----

    [Fact]
    public void OnlyTheGhostButton_DropsTheThemeRelief()
    {
        var offenders = Rules()
            .Where(rule => Regex.IsMatch(rule.Selector, @"\.omni-(split-)?button--(?!ghost)[a-z]+"))
            .Where(rule => rule.Body.Contains("--omni-button-shadow:", StringComparison.Ordinal) || rule.Body.Contains("box-shadow: 0 0 #0000", StringComparison.Ordinal))
            .Select(rule => rule.Selector)
            .ToList();

        Assert.Empty(offenders);
        Assert.Contains("box-shadow: 0 0 #0000", Body(".omni-button--ghost:active:not(:disabled)"), StringComparison.Ordinal);
    }

    [Fact]
    public void SecondaryButton_IsTheNeutralFillWithThePageTextAndNoBorder()
    {
        var secondary = Body(".omni-button--secondary");
        Assert.Equal("var(--omni-color-neutral-fill)", Value(secondary, "--omni-button-fill"));
        Assert.Equal("var(--omni-color-neutral-fill-hover)", Value(secondary, "--omni-button-fill-hover"));
        Assert.Equal("var(--omni-color-neutral-fill-active)", Value(secondary, "--omni-button-fill-active"));
        Assert.Equal("var(--omni-color-text)", Value(secondary, "--omni-button-ink"));
        Assert.DoesNotContain("border", secondary, StringComparison.Ordinal);
    }

    // ---- T4: the end padding gives the letter spacing back ----

    [Fact]
    public void LetterSpacing_DefaultsToZeroEmAndIsSubtractedFromTheEndPadding()
    {
        Assert.Equal("0em", Value(ShapeDefaults(), "--omni-button-letter-spacing"));
        Assert.Equal(
            "var(--omni-button-padding-inline, var(--omni-button-pad-x)) calc(var(--omni-button-padding-inline, var(--omni-button-pad-x)) - var(--omni-button-letter-spacing, 0em))",
            Value(Rules().Single(rule => rule.Selector == ".omni-button" && rule.Body.Contains("padding-inline", StringComparison.Ordinal)).Body, "padding-inline"));
        Assert.Equal(
            "var(--omni-space-md) calc(var(--omni-space-md) - var(--omni-button-letter-spacing, 0em))",
            Value(Rules().Single(rule => rule.Selector == ".omni-tabs__tab" && rule.Body.Contains("padding-inline", StringComparison.Ordinal)).Body, "padding-inline"));
        Assert.Contains("- var(--omni-button-letter-spacing, 0em))", Body(".omni-toggle-button,\n.omni-split-button__main"), StringComparison.Ordinal);
    }

    // ---- T7, T12: progress label and circle ----

    [Fact]
    public void ProgressLabelAndCircle_DoNotShrinkUnderTheirContent()
    {
        Assert.Equal("none", Value(Body(".omni-progress__label,\n.omni-progress__circle"), "flex"));
    }

    [Fact]
    public void ProgressCircle_TurnsFromItsRestThroughExactlyOneTurn()
    {
        var rest = Regex.Match(Body(".omni-progress__circle"), @"transform: rotate\((?<deg>-?\d+)deg\)");
        var turn = Regex.Match(Css, @"@keyframes omni-spin-circle \{ from \{ transform: rotate\((?<from>-?\d+)deg\); \} to \{ transform: rotate\((?<to>-?\d+)deg\); \} \}");

        Assert.True(rest.Success && turn.Success, "The circle's rest rotation or its keyframes are missing.");
        Assert.Equal(rest.Groups["deg"].Value, turn.Groups["from"].Value);
        Assert.Equal(360, int.Parse(turn.Groups["to"].Value, CultureInfo.InvariantCulture) - int.Parse(turn.Groups["from"].Value, CultureInfo.InvariantCulture));
        Assert.Contains("animation: omni-spin-circle", Body(".omni-progress--indeterminate .omni-progress__circle"), StringComparison.Ordinal);
    }

    // ---- T11: the busy veil covers the border ----

    [Fact]
    public void BusyVeil_CoversTheBorderWithoutClippingTheControl()
    {
        var host = Body(".omni-busy,\n.btn-busy");
        Assert.DoesNotContain("overflow", host, StringComparison.Ordinal);
        Assert.Contains("position: relative", host, StringComparison.Ordinal);

        var veil = Body(".omni-busy::after,\n.btn-busy::after");
        Assert.Equal("calc(-1 * var(--omni-button-border-width, 1px))", Value(veil, "inset"));
        Assert.Equal("inherit", Value(veil, "border-radius"));
    }

    // ---- T22, T24: one fill and one ink per intention for buttons, solid badges and alerts ----

    [Theory]
    [InlineData(OmniTone.Accent, OmniButtonVariant.Primary)]
    [InlineData(OmniTone.Info, OmniButtonVariant.Info)]
    [InlineData(OmniTone.Success, OmniButtonVariant.Success)]
    [InlineData(OmniTone.Warning, OmniButtonVariant.Warning)]
    [InlineData(OmniTone.Danger, OmniButtonVariant.Danger)]
    public void SolidBadge_TakesExactlyTheFillAndInkOfTheMatchingButton(OmniTone badge, OmniButtonVariant button)
    {
        var solid = badge == OmniTone.Accent ? Body(".omni-badge--solid") : Body($".omni-badge--solid.omni-badge--{badge.ToString().ToLowerInvariant()}");
        var fills = Body($".omni-button--{button.ToString().ToLowerInvariant()}");

        Assert.Equal(Value(fills, "--omni-button-fill"), Value(solid, "background"));
        Assert.Equal(Value(fills, "--omni-button-ink"), Value(solid, "color"));
    }

    /// <summary>
    /// recette R-347: the solid neutral badge took the secondary button's grey, 1.25:1 against a dark
    /// grid surface, so a status badge vanished into its row. It is the one solid badge that departs from its
    /// button: muted text as fill, surface as ink, a pair <see cref="ThemeContrastMatrixTests"/> holds at 4.5:1
    /// on every surface of the 200 sets (above the 3:1 of a component and the 4.5:1 of its text).
    /// </summary>
    [Fact]
    public void SolidNeutralBadge_StandsOutFromEverySurface()
    {
        var solid = Body(".omni-badge--solid.omni-badge--neutral");

        Assert.Equal("var(--omni-color-text-muted)", Value(solid, "background"));
        Assert.Equal("var(--omni-color-surface)", Value(solid, "color"));
    }

    [Fact]
    public void EveryBadgeVariant_HasItsTonalInk()
    {
        foreach (var variant in Enum.GetNames<OmniTone>().Select(name => name.ToLowerInvariant()))
        {
            Assert.Contains("--omni-badge-ink:", Body($".omni-badge--{variant}"), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Badge_RendersTheSolidFillAndTheInfoIntention()
    {
        var badge = Render<OmniBadge>(parameters => parameters
            .Add(component => component.Tone, OmniTone.Info)
            .Add(component => component.Fill, OmniFill.Solid)
            .AddChildContent("En revue"));

        var classes = badge.Find(".omni-badge").ClassList;
        Assert.Contains("omni-badge--info", classes);
        Assert.Contains("omni-badge--solid", classes);
    }

    [Theory]
    [InlineData(OmniSeverity.Info, OmniButtonVariant.Info)]
    [InlineData(OmniSeverity.Success, OmniButtonVariant.Success)]
    [InlineData(OmniSeverity.Warning, OmniButtonVariant.Warning)]
    [InlineData(OmniSeverity.Danger, OmniButtonVariant.Danger)]
    public void Alert_TakesExactlyTheFillAndInkOfTheButtonOfItsIntention(OmniSeverity severity, OmniButtonVariant button)
    {
        var alert = Body($".omni-alert--{severity.ToString().ToLowerInvariant()}");
        var fills = Body($".omni-button--{button.ToString().ToLowerInvariant()}");

        Assert.Equal(Value(fills, "--omni-button-fill"), Value(alert, "--omni-alert-fill"));
        Assert.Equal(Value(fills, "--omni-button-ink"), Value(alert, "--omni-alert-on"));
    }

    [Fact]
    public void SolidAlert_IsDrawnLikeAButtonWithoutAnySideBar()
    {
        var filled = Body(".omni-alert--solid");
        Assert.Equal("var(--omni-alert-fill)", Value(filled, "background"));
        Assert.Equal("var(--omni-alert-on)", Value(filled, "color"));
        // A theme may flatten it (--omni-alert-shadow, Aplat); the package draws it otherwise.
        Assert.StartsWith("var(--omni-alert-shadow, inset 0 1px 0 rgb(255 255 255 / 14%), 0 1px 2px var(--omni-elevation-shadow", Value(filled, "box-shadow"), StringComparison.Ordinal);
        Assert.Contains("color-mix(in srgb, var(--omni-alert-fill) 60%, transparent)", Value(filled, "box-shadow"), StringComparison.Ordinal);
        Assert.Equal("var(--omni-alert-radius, var(--omni-radius))", Value(Body(".omni-alert"), "border-radius"));

        var sideBars = Rules()
            .Where(rule => rule.Selector.Contains(".omni-alert", StringComparison.Ordinal))
            .Where(rule => Regex.IsMatch(rule.Body, @"border-(inline-start|inline-end|left|right)\s*:"))
            .Select(rule => rule.Selector)
            .ToList();
        Assert.Empty(sideBars);
    }

    [Fact]
    public void AlertDiscTitleAndClose_ShareTheDiscSizeInEveryDensity()
    {
        Assert.Equal("var(--omni-alert-icon)", Value(Body(".omni-alert__title"), "line-height"));
        foreach (var part in new[] { ".omni-alert__icon", ".omni-alert__dismiss" })
        {
            Assert.Equal("var(--omni-alert-icon)", Value(Body(part), "block-size"));
            Assert.Equal("var(--omni-alert-icon)", Value(Body(part), "inline-size"));
        }

        Assert.Equal("var(--omni-alert-glyph)", Value(Body(".omni-alert__icon svg"), "block-size"));

        // The 44 px target survives a smaller visible close button.
        Assert.Equal("min(0px, calc((var(--omni-alert-icon) - 2.75rem) / 2))", Value(Body(".omni-alert__dismiss::before"), "inset"));

        foreach (var (selector, icon, glyph) in new[]
                 {
                     (":root,\n[data-omni-density=\"comfortable\"]", "1.5rem", "1rem"),
                     ("[data-omni-density=\"compact\"]", "1.125rem", "0.75rem"),
                     ("[data-omni-density=\"spacious\"]", "1.75rem", "1.125rem")
                 })
        {
            Assert.Equal(icon, Value(Body(selector), "--omni-alert-icon"));
            Assert.Equal(glyph, Value(Body(selector), "--omni-alert-glyph"));
        }
    }

    [Theory]
    [InlineData(OmniSeverity.Info, "M12 11v6M12 7h.01")]
    [InlineData(OmniSeverity.Success, "m6.5 12.5 3.5 3.5 7.5-8")]
    [InlineData(OmniSeverity.Warning, "M12 7v6M12 17h.01")]
    [InlineData(OmniSeverity.Danger, "M8 8l8 8M16 8l-8 8")]
    public void Alert_DrawsTheSeverityGlyphAloneInItsDisc(OmniSeverity severity, string path)
    {
        var alert = Render<OmniAlert>(parameters => parameters
            .Add(component => component.Severity, severity)
            .Add(component => component.Fill, OmniFill.Solid)
            .Add(component => component.Title, "Titre")
            .AddChildContent("Message."));

        var glyph = alert.Find(".omni-alert__icon > svg.omni-alert__glyph");
        Assert.Equal("0 0 24 24", glyph.GetAttribute("viewBox"));
        Assert.Equal(path, Assert.Single(glyph.QuerySelectorAll("path")).GetAttribute("d"));
        Assert.Equal("true", alert.Find(".omni-alert__icon").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Alert_PutsTheConsumerIconInTheDiscInsteadOfTheGlyph()
    {
        var alert = Render<OmniAlert>(parameters => parameters
            .Add(component => component.Icon, builder => builder.AddMarkupContent(0, "<svg class=\"probe-icon\"></svg>"))
            .AddChildContent("Message."));

        Assert.Single(alert.FindAll(".omni-alert__icon > .probe-icon"));
        Assert.Empty(alert.FindAll(".omni-alert__glyph"));
    }

    // ---- T14: the layer tokens, each with the previous value as its fallback ----

    [Theory]
    [InlineData(".omni-card", "background", "var(--omni-card-background, var(--omni-color-surface))")]
    [InlineData(".omni-card", "border", "var(--omni-card-border-width, var(--omni-border-width)) solid var(--omni-card-border-color, var(--omni-color-border))")]
    [InlineData(".omni-dialog", "background", "var(--omni-dialog-background, var(--omni-card-background, var(--omni-color-surface)))")]
    [InlineData(".omni-dialog", "box-shadow", "inset 0 0 0 var(--omni-border-width) var(--omni-card-border-color, transparent), var(--omni-overlay-shadow, var(--omni-shadow-lg))")]
    [InlineData(".omni-menu", "box-shadow", "var(--omni-overlay-shadow, var(--omni-shadow-md))")]
    [InlineData(".omni-popover__panel", "box-shadow", "var(--omni-overlay-shadow, var(--omni-shadow-md))")]
    [InlineData(".omni-data-grid__popover-panel", "box-shadow", "var(--omni-overlay-shadow, var(--omni-shadow-md))")]
    [InlineData(".omni-combo__list", "box-shadow", "var(--omni-overlay-shadow, var(--omni-shadow-md))")]
    [InlineData(".omni-autocomplete__results", "box-shadow", "var(--omni-overlay-shadow, var(--omni-shadow-md))")]
    [InlineData(".omni-multi-select-compact__panel", "box-shadow", "var(--omni-overlay-shadow, var(--omni-shadow-md-strong))")]
    [InlineData(".omni-mindmap__menu", "box-shadow", "var(--omni-overlay-shadow, var(--omni-shadow-md))")]
    public void LayerTokens_FallBackToThePreviousLook(string selector, string property, string expected)
    {
        Assert.Equal(expected, Value(Body(selector), property));
    }

    // ---- The hooks of Relief, Givre and Aplat, each neutral when a theme does not set it (Givre's frosted cards included) ----

    [Theory]
    [InlineData(".omni-theme-scope", "background-image", "var(--omni-backdrop, none)")]
    [InlineData(".omni-theme-scope", "isolation", "var(--omni-scope-isolation, auto)")]
    [InlineData(".omni-input", "box-shadow", "var(--omni-input-shadow, 0 0 #0000)")]
    [InlineData(".omni-input:focus-visible", "box-shadow", "var(--omni-focus-ring), var(--omni-input-shadow, 0 0 #0000)")]
    [InlineData(".omni-overlay", "backdrop-filter", "var(--omni-scrim-filter, none)")]
    [InlineData(".omni-input", "background", "var(--omni-input-background, var(--omni-color-surface))")]
    [InlineData(".omni-input", "border", "var(--omni-border-width) solid var(--omni-input-border-color, var(--omni-color-border))")]
    [InlineData(".omni-password", "background", "var(--omni-input-background, var(--omni-color-surface))")]
    [InlineData(".omni-password", "border", "var(--omni-border-width) solid var(--omni-input-border-color, var(--omni-color-border))")]
    public void ThemeHooks_AreNeutralWithoutATheme(string selector, string property, string expected)
    {
        Assert.Equal(expected, Value(Body(selector), property));
    }

    [Fact]
    public void ThemeHooks_AreResetOnEveryScope()
    {
        var body = Body("[data-omni-theme]");
        Assert.Equal("none", Value(body, "--omni-backdrop"));
        Assert.Equal("auto", Value(body, "--omni-scope-isolation"));
        Assert.Equal("none", Value(body, "--omni-card-filter"));
        Assert.Equal("0 0 #0000", Value(body, "--omni-input-shadow"));
        Assert.Equal("none", Value(body, "--omni-scrim-filter"));
        Assert.Equal("initial", Value(body, "--omni-dialog-background"));
        Assert.Equal("initial", Value(body, "--omni-input-background"));
        Assert.Equal("initial", Value(body, "--omni-input-border-color"));
        Assert.Equal("initial", Value(body, "--omni-grid-background"));
        Assert.Equal("initial", Value(body, "--omni-alert-shadow"));
    }

    /// <summary>
    /// Givre's frost is one hook on the card surfaces (--omni-card-filter): every surface drawn on the
    /// card fill is positioned and carries the pseudo-element, which paints nothing without the hook.
    /// The two hooks that used to have to be set with it are gone.
    /// </summary>
    [Fact]
    public void FrostedSurfaces_ShareOneHook()
    {
        string[] surfaces = [".omni-card", ".omni-stat-tile", ".omni-settings-tile", ".omni-appearance-settings__row", ".omni-selectable-card", ".omni-upload__file"];
        var positioned = Rules().Single(rule => rule.Selector.StartsWith(".omni-card,", StringComparison.Ordinal) && rule.Body.Contains("position", StringComparison.Ordinal));
        var frost = Rules().Single(rule => rule.Selector.StartsWith(".omni-card::before,", StringComparison.Ordinal));

        Assert.Equal("relative", Value(positioned.Body, "position"));
        Assert.Equal(surfaces, positioned.Selector.Split(",\n"));
        Assert.Equal(surfaces.Select(surface => surface + "::before"), frost.Selector.Split(",\n"));
        Assert.Equal("var(--omni-card-filter, none)", Value(frost.Body, "backdrop-filter"));
        Assert.Equal("\"\"", Value(frost.Body, "content"));
        Assert.Equal("-1", Value(frost.Body, "z-index"));
        Assert.Equal("none", Value(frost.Body, "pointer-events"));

        Assert.DoesNotContain("--omni-card-position", Css, StringComparison.Ordinal);
        Assert.DoesNotContain("--omni-card-frost", Css, StringComparison.Ordinal);
        Assert.All(OmniThemePresets.All, preset =>
        {
            Assert.False(preset.Shape.ContainsKey("--omni-card-position"), preset.Name);
            Assert.False(preset.Shape.ContainsKey("--omni-card-frost"), preset.Name);
            Assert.Equal(preset.Shape.ContainsKey("--omni-card-filter"), preset.Shape.ContainsKey("--omni-scope-isolation"));
        });
    }

    /// <summary>
    /// A theme scope nested in another inherits nothing of the outer theme's shape: every shape token a
    /// theme of the catalogue sets is declared again on every scope, by the generated block (the tokens
    /// of the shipped theme), the theme hooks or the shape defaults. Palette and mode tokens are the
    /// generated colour blocks' concern; Givre's field stops are read only inside --omni-backdrop.
    /// </summary>
    [Fact]
    public void EveryShapeTokenOfTheCatalogue_IsDeclaredAgainOnEveryScope()
    {
        var shipped = OmniThemePresets.All[0].Shape.Keys;
        var hooks = Body("[data-omni-theme]");
        var defaults = ShapeDefaults();
        var missing = OmniThemePresets.All
            .SelectMany(preset => preset.Shape.Keys.Concat(preset.DarkShape.Keys))
            .Distinct(StringComparer.Ordinal)
            .Where(name => !name.StartsWith("--omni-color-", StringComparison.Ordinal)
                && !name.StartsWith("--omni-elevation-", StringComparison.Ordinal)
                && !name.StartsWith("--omni-backdrop-", StringComparison.Ordinal))
            .Where(name => !shipped.Contains(name))
            .Where(name => !Declares(hooks, name) && !Declares(defaults, name))
            .ToList();

        Assert.Empty(missing);
        Assert.Equal("1px", Value(defaults, "--omni-border-width"));
        Assert.Equal("initial", Value(defaults, "--omni-button-press-transform"));
        Assert.Equal("initial", Value(defaults, "--omni-heading-font-family"));
        Assert.Equal("2rem", Value(defaults, "--omni-font-size-h1"));
        Assert.Equal("rgb(0 0 0 / 55%)", Value(defaults, "--omni-color-overlay"));
    }

    /// <summary>
    /// One ring per state, in tokens a theme can restyle: the focus ring, its danger form around an
    /// invalid field and its inset form where an outer ring would be clipped. No rule writes a ring of
    /// its own, and a theme that draws its own ring draws all three.
    /// </summary>
    [Fact]
    public void FocusRings_AreThreeTokensThatEveryThemeRingSetsTogether()
    {
        var defaults = ShapeDefaults();
        Assert.StartsWith("0 0 0 0.2rem color-mix(in srgb, var(--omni-color-accent)", Value(defaults, "--omni-focus-ring"), StringComparison.Ordinal);
        Assert.StartsWith("0 0 0 0.2rem color-mix(in srgb, var(--omni-color-danger)", Value(defaults, "--omni-focus-ring-danger"), StringComparison.Ordinal);
        Assert.StartsWith("inset 0 0 0 0.2rem", Value(defaults, "--omni-focus-ring-inset"), StringComparison.Ordinal);

        var handWritten = Rules()
            .Where(rule => rule.Selector.Contains(":focus", StringComparison.Ordinal))
            .Where(rule => Regex.IsMatch(rule.Body, @"inset var\(--omni-focus-ring\)|0 0 0 0\.2rem|outline:\s*2px solid var\(--omni-color-accent\)"))
            .Select(rule => rule.Selector)
            .ToList();
        Assert.Empty(handWritten);
        Assert.Contains("var(--omni-focus-ring-danger)", Body(".omni-input[aria-invalid=\"true\"]:focus-visible"), StringComparison.Ordinal);
        Assert.Contains("var(--omni-focus-ring-danger)", Body(".omni-password--invalid:focus-within"), StringComparison.Ordinal);
        Assert.Contains("var(--omni-focus-ring-inset)", Body(".omni-password__toggle:focus-visible"), StringComparison.Ordinal);

        Assert.All(OmniThemePresets.All.Where(preset => preset.Shape.ContainsKey("--omni-focus-ring")), preset =>
        {
            Assert.True(preset.Shape.ContainsKey("--omni-focus-ring-danger"), preset.Name);
            Assert.True(preset.Shape.ContainsKey("--omni-focus-ring-inset"), preset.Name);
        });
    }

    /// <summary>Every control named by the plan takes the shared ring, the button-like ones beside their relief.</summary>
    [Theory]
    [InlineData(".omni-pager__button")]
    [InlineData(".omni-steps__button")]
    [InlineData(".omni-stack-scroll__button")]
    [InlineData(".omni-tabs__tab")]
    [InlineData(".omni-tabs__scroll")]
    [InlineData(".omni-tree__toggle")]
    [InlineData(".omni-tree__select")]
    [InlineData(".omni-fieldset__summary")]
    [InlineData(".omni-notification__dismiss")]
    [InlineData(".omni-notification__more")]
    [InlineData(".omni-multi-select-compact__clear")]
    [InlineData(".omni-toggle-button")]
    [InlineData(".omni-split-button__main")]
    [InlineData(".omni-split-button__toggle")]
    public void Controls_TakeTheSharedFocusRing(string control)
    {
        var ring = Rules().Where(rule => rule.Selector.Contains(":focus-visible", StringComparison.Ordinal)
                && Regex.IsMatch(rule.Selector, Regex.Escape(control) + @"[,)\s:]"))
            .Select(rule => rule.Body)
            .FirstOrDefault(body => body.Contains("var(--omni-focus-ring)", StringComparison.Ordinal));

        Assert.True(ring is not null, $"{control} has no focus ring.");
        Assert.Contains("outline: none", ring, StringComparison.Ordinal);
    }

    /// <summary>
    /// The layers are one scale of tokens: no surface writes a layer number of its own, and the
    /// connection overlay, which says nothing under it can be acted on, stands above windows and
    /// tooltips, themselves above dialogs.
    /// </summary>
    [Fact]
    public void Layers_AreOneScaleWithTheConnectionOverlayOnTop()
    {
        var root = Body(":root");
        int Layer(string name) => int.Parse(Value(root, name), CultureInfo.InvariantCulture);

        Assert.True(Layer("--omni-z-blocking") > Layer("--omni-z-tooltip"));
        Assert.True(Layer("--omni-z-tooltip") > Layer("--omni-z-window"));
        Assert.True(Layer("--omni-z-window") > Layer("--omni-z-toast"));
        Assert.True(Layer("--omni-z-toast") > Layer("--omni-z-overlay"));
        Assert.True(Layer("--omni-z-overlay") > Layer("--omni-z-popover"));
        Assert.Equal("var(--omni-z-blocking)", Value(Body(".omni-connection-overlay"), "z-index"));
        Assert.Equal("var(--omni-z-window)", Value(BodyWith(".omni-window-layer", "z-index"), "z-index"));
        Assert.Equal("var(--omni-z-tooltip)", Value(Body(".omni-title-tooltip"), "z-index"));
        Assert.Equal("var(--omni-z-overlay)", Value(Body(".omni-overlay"), "z-index"));

        // A literal of two digits or more is a layer written by hand; 1 to 4 order the parts of one component.
        var literals = Rules().Where(rule => Regex.IsMatch(rule.Body, @"z-index:\s*\d{2,}")).Select(rule => rule.Selector).ToList();
        Assert.Empty(literals);
    }

    /// <summary>
    /// The owner's rule: no accent bar on one edge. A rule on the start edge is a structural hairline of
    /// the border width, or none; no inset shadow is offset sideways to draw one.
    /// </summary>
    [Fact]
    public void NoRule_DrawsASideBar()
    {
        var edges = Rules()
            .SelectMany(rule => Regex.Matches(rule.Body, @"border-(?:inline-start|inline-end|left|right)\s*:\s*(?<value>[^;]+)").Select(match => (rule.Selector, Value: match.Groups["value"].Value.Trim())))
            .Where(edge => !edge.Value.StartsWith('0') && !edge.Value.StartsWith("var(--omni-border-width", StringComparison.Ordinal))
            .Select(edge => $"{edge.Selector}: {edge.Value}")
            .ToList();
        var insetBars = Rules()
            .Where(rule => Regex.IsMatch(rule.Body, @"inset\s+-?(?!0[\s)])[0-9.]+(?:rem|px|em)\s+0\s+0"))
            .Select(rule => rule.Selector)
            .ToList();

        Assert.Empty(edges);
        Assert.Empty(insetBars);
        Assert.Contains("font-weight: 600", Body(".omni-log-viewer__line--current"), StringComparison.Ordinal);
        Assert.Contains("background", Body(".omni-rich-text blockquote"), StringComparison.Ordinal);
    }

    /// <summary>Selected, pressed and current take one fill everywhere: the accent fill and its ink.</summary>
    [Theory]
    [InlineData(".omni-select-bar__item--selected")]
    [InlineData(".omni-toggle-button--pressed")]
    [InlineData(".omni-steps__item--selected .omni-steps__number")]
    [InlineData(".omni-pager__button[aria-current=\"page\"]")]
    public void SelectedStates_TakeTheAccentFillAndItsInk(string selector)
    {
        var body = Body(selector);
        Assert.Equal("var(--omni-color-accent-fill)", Value(body, "background"));
        Assert.Equal("var(--omni-color-on-accent-fill)", Value(body, "color"));
    }

    [Fact]
    public void TranslucentFloatingLayers_FallBackToTheOpaqueSurfaceWithoutBackdropFilter()
    {
        Assert.Matches(
            @"@supports not \(\(backdrop-filter: blur\(1px\)\) or \(-webkit-backdrop-filter: blur\(1px\)\)\) \{\s*\* \{ --omni-overlay-background: var\(--omni-color-surface\); \}",
            Uncommented(Css));
    }

    [Theory]
    [InlineData(".omni-notification")]
    [InlineData(".omni-menu")]
    [InlineData(".omni-popover__panel")]
    [InlineData(".omni-data-grid__popover-panel")]
    [InlineData(".omni-combo__list")]
    [InlineData(".omni-autocomplete__results")]
    [InlineData(".omni-multi-select-compact__panel")]
    [InlineData(".omni-mindmap__menu")]
    public void FloatingSurfaces_ReadTheOverlayLayerWithTheirPreviousSurfaceAsFallback(string selector)
    {
        var body = Rules().First(rule => rule.Selector == selector && rule.Body.Contains("background", StringComparison.Ordinal)).Body;
        Assert.Equal("var(--omni-overlay-background, var(--omni-color-surface))", Value(body, "background"));
        Assert.Equal("var(--omni-overlay-filter, none)", Value(body, "backdrop-filter"));
        Assert.Contains("var(--omni-card-border-color, var(--omni-color-border))", Value(body, "border"), StringComparison.Ordinal);
    }

    [Fact]
    public void Dialog_KeepsNoBackdropFilterSoFixedDescendantsKeepTheViewport()
    {
        Assert.DoesNotContain("backdrop-filter", Body(".omni-dialog"), StringComparison.Ordinal);
    }

    [Fact]
    public void IconDisc_IsAGreyDiscOfThePalette()
    {
        var disc = Body(".omni-disc");
        Assert.Equal("var(--omni-color-neutral-fill)", Value(disc, "background"));
        Assert.Equal("var(--omni-radius-circle)", Value(disc, "border-radius"));
        Assert.Equal(Value(disc, "block-size"), Value(disc, "inline-size"));
    }

    // ---- T16: the small radius ----

    [Theory]
    [InlineData(".omni-badge")]
    [InlineData(".omni-checkbox-nullable__indicator")]
    public void BadgesAndCheckboxes_TakeTheThemesSmallRadius(string selector)
    {
        Assert.Equal("var(--omni-radius-sm)", Value(Body(selector), "border-radius"));
        // The half radius used to stand in for the small one in a dozen rules: the token is always declared.
        Assert.DoesNotContain("var(--omni-radius) / 2", Css, StringComparison.Ordinal);
    }

    // ---- helpers ----

    internal static IEnumerable<(string Selector, string Body)> Rules() =>
        Rule().Matches(Css).Select(match => (Selector: Normalise(match.Groups["selector"].Value), Body: match.Groups["body"].Value));

    /// <summary>The body of the first rule whose whole selector list is exactly <paramref name="selector"/>.</summary>
    internal static string Body(string selector)
    {
        var match = Rules().FirstOrDefault(rule => rule.Selector == selector);
        Assert.True(match.Body is not null, $"No rule for {selector.Replace("\n", " ", StringComparison.Ordinal)}.");
        return match.Body;
    }

    internal static string Value(string body, string property)
    {
        var match = Regex.Match(body, @"(?:^|;|\{)\s*" + Regex.Escape(property) + @"\s*:\s*(?<value>[^;]+)");
        Assert.True(match.Success, $"No {property} declaration in: {body.Trim()}");
        return Regex.Replace(match.Groups["value"].Value.Trim(), @"\s+", " ");
    }

    // A selector list keeps one line break after each comma, whatever its indentation inside an
    // at-rule, so a multi-line list is looked up as written.
    private static string Normalise(string selector) =>
        Regex.Replace(selector.Trim(), @"\s*\n\s*", "\n");

    /// <summary>The shape defaults declared on the root and again on every theme scope.</summary>
    internal static string ShapeDefaults() => BodyWith(":root,\n[data-omni-theme]", "--omni-border-width:");

    private static bool Declares(string body, string name) =>
        Regex.IsMatch(body, @"(?:^|;|\{)\s*" + Regex.Escape(name) + @"\s*:");

    private static string BodyWith(string selector, string declaration)
    {
        var match = Rules().FirstOrDefault(rule => rule.Selector == selector && rule.Body.Contains(declaration, StringComparison.Ordinal));
        Assert.True(match.Body is not null, $"No rule for {selector} declaring {declaration}.");
        return match.Body;
    }

    internal static string Uncommented(string css) =>
        Regex.Replace(css, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline).ReplaceLineEndings("\n");

    internal static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OmniEurope.Blazor.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
