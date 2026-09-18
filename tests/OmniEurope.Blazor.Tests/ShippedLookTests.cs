using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Non-regression of the shipped look ported from the PLAN-008 mockup (lot 7): button states, the
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
        Assert.Contains("transform 80ms", transition, StringComparison.Ordinal);
        Assert.Contains("box-shadow 120ms", transition, StringComparison.Ordinal);
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
        Assert.Equal("0em", Value(Body(":root"), "--omni-button-letter-spacing"));
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
    [InlineData(OmniBadgeVariant.Accent, OmniButtonVariant.Primary)]
    [InlineData(OmniBadgeVariant.Neutral, OmniButtonVariant.Secondary)]
    [InlineData(OmniBadgeVariant.Info, OmniButtonVariant.Info)]
    [InlineData(OmniBadgeVariant.Success, OmniButtonVariant.Success)]
    [InlineData(OmniBadgeVariant.Warning, OmniButtonVariant.Warning)]
    [InlineData(OmniBadgeVariant.Danger, OmniButtonVariant.Danger)]
    public void SolidBadge_TakesExactlyTheFillAndInkOfTheMatchingButton(OmniBadgeVariant badge, OmniButtonVariant button)
    {
        var solid = badge == OmniBadgeVariant.Accent ? Body(".omni-badge--solid") : Body($".omni-badge--solid.omni-badge--{badge.ToString().ToLowerInvariant()}");
        var fills = Body($".omni-button--{button.ToString().ToLowerInvariant()}");

        Assert.Equal(Value(fills, "--omni-button-fill"), Value(solid, "background"));
        Assert.Equal(Value(fills, "--omni-button-ink"), Value(solid, "color"));
    }

    [Fact]
    public void EveryBadgeVariant_HasItsTonalInk()
    {
        foreach (var variant in Enum.GetNames<OmniBadgeVariant>().Select(name => name.ToLowerInvariant()))
        {
            Assert.Contains("--omni-badge-ink:", Body($".omni-badge--{variant}"), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Badge_RendersTheSolidFillAndTheInfoIntention()
    {
        var badge = Render<OmniBadge>(parameters => parameters
            .Add(component => component.Variant, OmniBadgeVariant.Info)
            .Add(component => component.Fill, OmniBadgeFill.Solid)
            .Add(component => component.Text, "En revue"));

        var classes = badge.Find(".omni-badge").ClassList;
        Assert.Contains("omni-badge--info", classes);
        Assert.Contains("omni-badge--solid", classes);
    }

    [Theory]
    [InlineData(OmniAlertSeverity.Info, OmniButtonVariant.Info)]
    [InlineData(OmniAlertSeverity.Success, OmniButtonVariant.Success)]
    [InlineData(OmniAlertSeverity.Warning, OmniButtonVariant.Warning)]
    [InlineData(OmniAlertSeverity.Danger, OmniButtonVariant.Danger)]
    public void Alert_TakesExactlyTheFillAndInkOfTheButtonOfItsIntention(OmniAlertSeverity severity, OmniButtonVariant button)
    {
        var alert = Body($".omni-alert--{severity.ToString().ToLowerInvariant()}");
        var fills = Body($".omni-button--{button.ToString().ToLowerInvariant()}");

        Assert.Equal(Value(fills, "--omni-button-fill"), Value(alert, "--omni-alert-fill"));
        Assert.Equal(Value(fills, "--omni-button-ink"), Value(alert, "--omni-alert-on"));
    }

    [Fact]
    public void FilledAlert_IsDrawnLikeAButtonWithoutAnySideBar()
    {
        var filled = Body(".omni-alert--filled");
        Assert.Equal("var(--omni-alert-fill)", Value(filled, "background"));
        Assert.Equal("var(--omni-alert-on)", Value(filled, "color"));
        Assert.StartsWith("inset 0 1px 0 rgb(255 255 255 / 14%), 0 1px 2px var(--omni-elevation-shadow", Value(filled, "box-shadow"), StringComparison.Ordinal);
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
    [InlineData(OmniAlertSeverity.Info, "M12 11v6M12 7h.01")]
    [InlineData(OmniAlertSeverity.Success, "m6.5 12.5 3.5 3.5 7.5-8")]
    [InlineData(OmniAlertSeverity.Warning, "M12 7v6M12 17h.01")]
    [InlineData(OmniAlertSeverity.Danger, "M8 8l8 8M16 8l-8 8")]
    public void Alert_DrawsTheSeverityGlyphAloneInItsDisc(OmniAlertSeverity severity, string path)
    {
        var alert = Render<OmniAlert>(parameters => parameters
            .Add(component => component.Severity, severity)
            .Add(component => component.Variant, OmniAlertVariant.Filled)
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
    [InlineData(".omni-dialog", "background", "var(--omni-card-background, var(--omni-color-surface))")]
    [InlineData(".omni-dialog", "box-shadow", "inset 0 0 0 var(--omni-border-width) var(--omni-card-border-color, transparent), var(--omni-overlay-shadow, var(--omni-shadow-lg))")]
    [InlineData(".omni-split-button__menu,\n.omni-context-menu__popup", "box-shadow", "var(--omni-overlay-shadow, var(--omni-shadow-md))")]
    [InlineData(".omni-popover__panel", "box-shadow", "var(--omni-overlay-shadow, var(--omni-shadow-md))")]
    [InlineData(".omni-profile-menu__items", "box-shadow", "var(--omni-overlay-shadow, var(--omni-shadow-md))")]
    [InlineData(".omni-data-grid__popover-panel", "box-shadow", "var(--omni-overlay-shadow, var(--omni-shadow-md))")]
    [InlineData(".omni-combo__list", "box-shadow", "var(--omni-overlay-shadow, var(--omni-shadow-md))")]
    [InlineData(".omni-autocomplete__results", "box-shadow", "var(--omni-overlay-shadow, var(--omni-shadow-md))")]
    [InlineData(".omni-multi-select-compact__panel", "box-shadow", "var(--omni-overlay-shadow, var(--omni-shadow-md-strong))")]
    [InlineData(".omni-mindmap__menu", "box-shadow", "var(--omni-overlay-shadow, var(--omni-shadow-md))")]
    public void LayerTokens_FallBackToThePreviousLook(string selector, string property, string expected)
    {
        Assert.Equal(expected, Value(Body(selector), property));
    }

    [Theory]
    [InlineData(".omni-notification")]
    [InlineData(".omni-split-button__menu,\n.omni-context-menu__popup")]
    [InlineData(".omni-popover__panel")]
    [InlineData(".omni-profile-menu__items")]
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
        Assert.Equal("var(--omni-radius-sm, calc(var(--omni-radius) / 2))", Value(Body(selector), "border-radius"));
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
