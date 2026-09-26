using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Non-regression of the second batch of the PLAN-008 mockup (lot 7): density (T21, T22), grids
/// (T15, T17 a and b), form states (T17 c, T18 c), settings tiles and the file field (T20, T21), the
/// notification mark and the shell (T18 b). Each test reads the source stylesheet, or renders the
/// component when the markup is what matters.
/// </summary>
public sealed class ShippedLookDensityTests : OmniBunitContext
{
    private const string ComfortableDensity = ":root,\n[data-omni-density=\"comfortable\"]";
    private const string CompactDensity = "[data-omni-density=\"compact\"]";
    private const string SpaciousDensity = "[data-omni-density=\"spacious\"]";

    /// <summary>The tokens of the mockup's density blocks, every one of them sized per level.</summary>
    private static readonly string[] DensityTokens =
    [
        "--omni-control-height", "--omni-control-font", "--omni-button-pad-x", "--omni-input-pad-y",
        "--omni-cell-pad-y", "--omni-card-pad", "--omni-alert-pad-y", "--omni-alert-pad-x",
        "--omni-item-pad-y", "--omni-tile-pad-y", "--omni-form-gap", "--omni-cal-cell", "--omni-pop-pad",
        "--omni-field-gap", "--omni-check-size", "--omni-switch-h", "--omni-icon-box",
        "--omni-badge-pad-y", "--omni-badge-height", "--omni-section-pad", "--omni-alert-icon", "--omni-alert-glyph"
    ];

    // ---- T21, T22: the density tokens ----

    [Fact]
    public void EveryDensityBlock_DeclaresTheSameSetOfTokens()
    {
        var comfortable = Declared(ShippedLookTests.Body(ComfortableDensity));
        var compact = Declared(ShippedLookTests.Body(CompactDensity));
        var spacious = Declared(ShippedLookTests.Body(SpaciousDensity));

        Assert.Equal(comfortable, compact);
        Assert.Equal(comfortable, spacious);
        Assert.Superset(DensityTokens.ToHashSet(StringComparer.Ordinal), comfortable);
    }

    [Theory]
    // The control tokens keep the mockup values at the drawn control size (scale 1).
    [InlineData("--omni-control-height", "calc(2.25rem * var(--omni-control-scale, 1))", "calc(1.625rem * var(--omni-control-scale, 1))", "calc(2.75rem * var(--omni-control-scale, 1))")]
    [InlineData("--omni-control-font", "calc(0.875rem * var(--omni-control-scale, 1))", "calc(0.75rem * var(--omni-control-scale, 1))", "calc(0.9375rem * var(--omni-control-scale, 1))")]
    [InlineData("--omni-cell-pad-y", "8px", "2px", "12px")]
    [InlineData("--omni-card-pad", "14px", "8px", "18px")]
    [InlineData("--omni-switch-h", "1.25rem", "1rem", "1.5rem")]
    [InlineData("--omni-icon-box", "2.25rem", "1.625rem", "2.75rem")]
    public void DensityLevels_TakeTheValuesOfTheMockup(string token, string comfortable, string compact, string spacious)
    {
        Assert.Equal(comfortable, ShippedLookTests.Value(ShippedLookTests.Body(ComfortableDensity), token));
        Assert.Equal(compact, ShippedLookTests.Value(ShippedLookTests.Body(CompactDensity), token));
        Assert.Equal(spacious, ShippedLookTests.Value(ShippedLookTests.Body(SpaciousDensity), token));
    }

    [Theory]
    [InlineData(".omni-button", "font-size", "var(--omni-control-font)")]
    [InlineData(".omni-button", "padding-inline", "var(--omni-button-pad-x)")]
    [InlineData(".omni-input", "font-size", "var(--omni-control-font)")]
    [InlineData(".omni-input", "padding", "var(--omni-input-pad-y)")]
    [InlineData(".omni-input", "min-height", "var(--omni-control-height)")]
    [InlineData(".omni-text-area__input", "min-height", "var(--omni-control-height)")]
    [InlineData(".omni-data-grid__table :is(th, td)", "padding", "var(--omni-cell-pad-y)")]
    [InlineData(".omni-card__body", "padding", "var(--omni-card-pad)")]
    [InlineData(".omni-alert", "padding", "var(--omni-alert-pad-y) var(--omni-alert-pad-x)")]
    [InlineData(".omni-split-button__item", "padding", "var(--omni-item-pad-y)")]
    [InlineData(".omni-profile-menu__item", "padding", "var(--omni-item-pad-y)")]
    [InlineData(".omni-profile-menu__items", "padding", "var(--omni-pop-pad)")]
    [InlineData(".omni-tabs__tab", "padding-block", "var(--omni-item-pad-y)")]
    [InlineData(".omni-settings-tile", "padding", "var(--omni-tile-pad-y)")]
    [InlineData(".omni-settings-tile__icon", "block-size", "var(--omni-icon-box)")]
    [InlineData(".omni-upload__file", "padding", "var(--omni-item-pad-y)")]
    [InlineData(".omni-upload__file-icon", "inline-size", "var(--omni-icon-box)")]
    [InlineData(".omni-upload__zone", "padding", "var(--omni-section-pad)")]
    [InlineData(".omni-badge", "padding", "var(--omni-badge-pad-y)")]
    [InlineData(".omni-badge", "min-height", "var(--omni-badge-height)")]
    [InlineData(".omni-badge", "min-width", "var(--omni-badge-height)")]
    [InlineData(".omni-notification", "padding", "var(--omni-alert-pad-y)")]
    [InlineData(".omni-dialog__content", "padding", "var(--omni-card-pad)")]
    [InlineData(".omni-form-field", "gap", "var(--omni-field-gap)")]
    [InlineData(".omni-disc", "block-size", "var(--omni-icon-box)")]
    [InlineData(".omni-switch__track", "block-size", "var(--omni-switch-h)")]
    [InlineData(".omni-switch__track", "inline-size", "calc(var(--omni-switch-h) * 1.8)")]
    [InlineData(".omni-switch__thumb", "block-size", "calc(var(--omni-switch-h) - 6px)")]
    [InlineData(".omni-switch--checked .omni-switch__thumb", "transform", "translateX(calc(var(--omni-switch-h) * 0.8))")]
    [InlineData(".omni-radio::before", "block-size", "0.5rem")]
    [InlineData(".omni-header", "padding", "var(--omni-item-pad-y)")]
    [InlineData(".omni-settings-section > .omni-card__body", "padding", "var(--omni-section-pad)")]
    [InlineData(".omni-calendar", "padding", "var(--omni-pop-pad)")]
    [InlineData(".omni-calendar", "--omni-button-size", "var(--omni-control-height)")]
    [InlineData(".omni-calendar", "background", "var(--omni-overlay-background")]
    [InlineData(".omni-calendar", "box-shadow", "var(--omni-overlay-shadow")]
    [InlineData(".omni-calendar__grid", "grid-template-columns", "var(--omni-cal-cell)")]
    [InlineData(".omni-calendar__day", "block-size", "var(--omni-cal-cell)")]
    [InlineData(".omni-calendar__day", "inline-size", "var(--omni-cal-cell)")]
    [InlineData(".omni-calendar__day", "font-size", "var(--omni-control-font)")]
    [InlineData(".omni-calendar__weekday", "padding-block", "var(--omni-item-pad-y)")]
    [InlineData(".omni-calendar__title", "font-size", "var(--omni-control-font)")]
    [InlineData(".omni-date__toggle", "block-size", "var(--omni-control-height)")]
    [InlineData(".omni-date__field .omni-input", "padding-inline-end", "var(--omni-control-height)")]
    [InlineData(".omni-time__col", "inline-size", "var(--omni-cal-cell)")]
    [InlineData(".omni-time__list", "max-block-size", "var(--omni-cal-cell)")]
    [InlineData(".omni-time__item", "padding", "var(--omni-item-pad-y)")]
    public void SizedComponents_ReadTheirSizeFromTheDensity(string selector, string property, string expected)
    {
        var rule = ShippedLookTests.Rules().Where(rule => rule.Selector == selector)
            .Select(rule => rule.Body)
            .FirstOrDefault(body => Regex.IsMatch(body, @"(?:^|;|\{)\s*" + Regex.Escape(property) + @"\s*:"));
        Assert.True(rule is not null, $"No {property} on {selector}.");
        Assert.Contains(expected, ShippedLookTests.Value(rule, property), StringComparison.Ordinal);
    }

    /// <summary>
    /// Aetheus recette R-345: in a data row, an icon-only button (the host's or the grid's own edit
    /// actions) is a square of the badge height, the token the status badges of the same row read.
    /// </summary>
    [Fact]
    public void GridRowIconButtons_AreSquaresOfTheBadgeHeight()
    {
        var rule = ShippedLookTests.Body(
            ".omni-data-grid__row > td :is(.omni-button:has(> .omni-button__content > .omni-icon:only-child), .omni-data-grid__icon-button)");

        foreach (var property in new[] { "--omni-button-size", "block-size", "inline-size", "min-inline-size" })
        {
            Assert.Equal("var(--omni-badge-height)", ShippedLookTests.Value(rule, property));
        }

        Assert.Equal("0", ShippedLookTests.Value(rule, "padding"));
    }

    [Fact]
    public void CheckBoxes_AndRadios_TakeTheDensityCheckSize()
    {
        var shared = ShippedLookTests.Rules().Single(rule => rule.Selector.Contains(".omni-radio", StringComparison.Ordinal)
            && rule.Selector.Contains(".omni-checkbox", StringComparison.Ordinal)
            && rule.Body.Contains("appearance: none", StringComparison.Ordinal)).Body;
        Assert.Equal("var(--omni-check-size)", ShippedLookTests.Value(shared, "block-size"));
        Assert.Equal("var(--omni-check-size)", ShippedLookTests.Value(shared, "inline-size"));
        Assert.Equal("var(--omni-check-size)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-checkbox-nullable__indicator"), "height"));
    }

    [Fact]
    public void DensityRules_RaiseNoSpecificity_TheOnlySelectorsNamingTheAttributeAreTheThreeBlocks()
    {
        var naming = ShippedLookTests.Rules()
            .Where(rule => rule.Selector.Contains("data-omni-density", StringComparison.Ordinal))
            .Select(rule => rule.Selector)
            .ToList();

        Assert.Equal([ComfortableDensity, CompactDensity, SpaciousDensity], naming);
    }

    // ---- T21: a Density parameter on the components that carry one ----

    [Theory]
    [InlineData("OmniButton")]
    [InlineData("OmniFormField")]
    [InlineData("OmniCard")]
    [InlineData("OmniAlert")]
    [InlineData("OmniPanelMenu")]
    [InlineData("OmniProfileMenu")]
    [InlineData("OmniContextMenu")]
    [InlineData("OmniTabs")]
    [InlineData("OmniSettingsTile")]
    [InlineData("OmniUpload")]
    public void Density_IsRenderedOnTheRootOnlyWhenSet(string component)
    {
        var inherited = RenderWithDensity(component, null);
        Assert.False(inherited.HasAttribute("data-omni-density"), $"{component} renders a density it was not given.");

        foreach (var density in Enum.GetValues<OmniDensity>())
        {
            var own = RenderWithDensity(component, density);
            Assert.Equal(density.ToString().ToLowerInvariant(), own.GetAttribute("data-omni-density"));
        }
    }

    [Fact]
    public void ContextMenu_CarriesItsDensityOnThePopupToo_WhichThePortalMovesOutOfTheTrigger()
    {
        var menu = Render<OmniContextMenu>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Density, OmniDensity.Spacious)
            .Add(component => component.TriggerContent, (RenderFragment)(builder => builder.AddContent(0, "Cible"))));

        Assert.Equal("spacious", menu.Find(".omni-context-menu__popup").GetAttribute("data-omni-density"));
    }

    private AngleSharp.Dom.IElement RenderWithDensity(string component, OmniDensity? density)
    {
        RenderFragment content = builder => builder.AddContent(0, "Contenu");
        return component switch
        {
            "OmniButton" => Render<OmniButton>(p => p.Add(c => c.Density, density).AddChildContent("Action")).Find("button"),
            "OmniFormField" => Render<OmniFormField>(p => p.Add(c => c.Density, density).Add(c => c.Text, "Nom")).Find(".omni-form-field"),
            "OmniCard" => Render<OmniCard>(p => p.Add(c => c.Density, density).Add(c => c.ChildContent, content)).Find(".omni-card"),
            "OmniAlert" => Render<OmniAlert>(p => p.Add(c => c.Density, density).Add(c => c.ChildContent, content)).Find(".omni-alert"),
            "OmniPanelMenu" => Render<OmniPanelMenu>(p => p.Add(c => c.Density, density)).Find(".omni-panel-menu"),
            "OmniProfileMenu" => Render<OmniProfileMenu>(p => p.Add(c => c.Density, density).Add(c => c.Summary, content)).Find(".omni-profile-menu"),
            "OmniContextMenu" => Render<OmniContextMenu>(p => p.Add(c => c.Density, density).Add(c => c.TriggerContent, content)).Find(".omni-context-menu"),
            "OmniTabs" => Render<OmniTabs>(p => p.Add(c => c.Density, density)).Find(".omni-tabs"),
            "OmniSettingsTile" => Render<OmniSettingsTile>(p => p.Add(c => c.Density, density).Add(c => c.Title, "Réglage")).Find(".omni-settings-tile"),
            "OmniUpload" => Render<OmniUpload>(p => p.Add(c => c.Density, density)).Find(".omni-upload"),
            _ => throw new ArgumentOutOfRangeException(nameof(component), component, null)
        };
    }

    // ---- T15, T17 a and b: grids ----

    [Fact]
    public void GridFrame_IsTheScrollingViewport_DressedLikeACard_WithAThinScrollbarTintedFromTheText()
    {
        var viewport = ShippedLookTests.Body(".omni-data-grid__viewport");
        Assert.Equal("var(--omni-grid-frame)", ShippedLookTests.Value(viewport, "background"));
        Assert.Equal("var(--omni-card-radius, var(--omni-radius))", ShippedLookTests.Value(viewport, "border-radius"));
        Assert.Equal("var(--omni-card-shadow, 0 0 #0000)", ShippedLookTests.Value(viewport, "box-shadow"));
        Assert.Equal("auto", ShippedLookTests.Value(viewport, "overflow"));
        Assert.Equal("thin", ShippedLookTests.Value(viewport, "scrollbar-width"));
        Assert.Equal("color-mix(in srgb, var(--omni-color-text) 28%, transparent) transparent", ShippedLookTests.Value(viewport, "scrollbar-color"));
        Assert.Contains("--omni-grid-frame: var(--omni-card-background, var(--omni-color-surface))", ShippedLookTests.Rules().First(rule => rule.Selector == ".omni-data-grid").Body, StringComparison.Ordinal);
    }

    [Fact]
    public void GridTable_UsesTheSeparateBorderModel_AndLeavesItsCellsTableCells()
    {
        var table = ShippedLookTests.Body(".omni-data-grid__table");
        Assert.Equal("separate", ShippedLookTests.Value(table, "border-collapse"));
        Assert.Equal("0", ShippedLookTests.Value(table, "border-spacing"));

        // Only the opt-in responsive layout, under its media query, turns cells into anything else.
        var display = ShippedLookTests.Rules()
            .Where(rule => Regex.IsMatch(rule.Selector, @"\btd\b") && Regex.IsMatch(rule.Body, @"(?:^|;)\s*display\s*:"))
            .Select(rule => rule.Selector)
            .ToList();
        Assert.All(display, selector => Assert.Contains("omni-data-grid--responsive", selector, StringComparison.Ordinal));
    }

    [Fact]
    public void GridHeader_IsThinStickyAndMuted_WithARuleAndATintOfItsOwn()
    {
        Assert.Equal("sticky", ShippedLookTests.Value(ShippedLookTests.Body(".omni-data-grid__table thead"), "position"));
        var head = ShippedLookTests.Body(".omni-data-grid thead > tr > *");
        Assert.Equal("var(--omni-header-fill)", ShippedLookTests.Value(head, "background"));
        Assert.Equal("var(--omni-color-text-muted)", ShippedLookTests.Value(head, "color"));
        Assert.Equal("var(--omni-border-width) solid var(--omni-color-border)", ShippedLookTests.Value(head, "border-block-end"));
        Assert.Equal("600", ShippedLookTests.Value(ShippedLookTests.Body(".omni-data-grid__table th"), "font-weight"));
        Assert.Contains("--omni-header-fill: color-mix(in srgb, var(--omni-color-text) 6%, var(--omni-grid-frame))", ShippedLookTests.Rules().Single(rule => rule.Selector == ".omni-data-grid" && rule.Body.Contains("--omni-header-fill:", StringComparison.Ordinal)).Body, StringComparison.Ordinal);
        // The shadow gradient drawn under the header by a pseudo-element is gone: the rule replaces it.
        Assert.DoesNotContain(ShippedLookTests.Rules(), rule => rule.Selector.Contains("thead > tr:last-child", StringComparison.Ordinal) && rule.Selector.Contains("::after", StringComparison.Ordinal));
    }

    [Fact]
    public void GridRows_AreSeparatedStripedOrNot_AndTheSelectedRowIsTintedAndBoldWithoutASideBar()
    {
        var separators = ShippedLookTests.Rules().Single(rule => rule.Selector.Contains(":not(.omni-data-grid--lines-none)", StringComparison.Ordinal));
        Assert.StartsWith(":where(", separators.Selector, StringComparison.Ordinal);
        Assert.Contains("color-mix(in srgb, var(--omni-color-border) 55%, transparent)", separators.Body, StringComparison.Ordinal);

        Assert.Equal("color-mix(in srgb, var(--omni-color-accent) 14%, transparent)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-data-grid tbody > .omni-data-grid__row--selected"), "--omni-row-fill"));
        Assert.Equal("600", ShippedLookTests.Value(ShippedLookTests.Body(".omni-data-grid tbody > .omni-data-grid__row--selected > td"), "font-weight"));
        var selectedRules = ShippedLookTests.Rules().Where(rule => rule.Selector.Contains("row--selected", StringComparison.Ordinal)).Select(rule => rule.Body).ToList();
        Assert.All(selectedRules, body => Assert.DoesNotMatch(@"box-shadow|border-inline|border-left|border-right", body));
    }

    [Fact]
    public void ActiveColumn_IsTintedAtSixPercent_LighterThanTheSelectedRow()
    {
        Assert.Contains("--omni-column-active-fill: color-mix(in srgb, var(--omni-color-accent) 6%, transparent)", ShippedLookTests.Rules().First(rule => rule.Selector == ".omni-data-grid").Body, StringComparison.Ordinal);
    }

    [Fact]
    public void CompactStatus_IsAUtilityDotInTheFillColourBeforeThePageText()
    {
        Assert.Equal("inline-flex", ShippedLookTests.Value(ShippedLookTests.Body(".omni-status"), "display"));
        var dot = ShippedLookTests.Body(".omni-status::before");
        Assert.Equal("var(--omni-status-dot, var(--omni-color-text-muted))", ShippedLookTests.Value(dot, "background"));
        Assert.Equal("0.5rem", ShippedLookTests.Value(dot, "block-size"));
        foreach (var intention in new[] { "success", "info", "warning", "danger" })
        {
            Assert.Equal($"var(--omni-color-{intention}-fill)", ShippedLookTests.Value(ShippedLookTests.Body($".omni-status--{intention}"), "--omni-status-dot"));
        }
    }

    [Fact]
    public void NumericColumns_AlignAtTheEnd_UnlessTheirAlignmentWasSet()
    {
        var grid = Render<NumericColumnsTestHost>();

        string[] Classes(string key) => grid.FindAll($"td[data-omni-col='{key}']").Select(cell => cell.ClassName ?? string.Empty).ToArray();
        var header = grid.FindAll("thead th").Single(cell => cell.TextContent.Contains("Durée", StringComparison.Ordinal));

        Assert.Contains("omni-data-grid__cell--numeric", header.ClassList);
        Assert.All(new[] { "name", "duration", "cost", "lambda", "templated" }, key => Assert.Equal(2, Classes(key).Length));
        Assert.All(Classes("duration"), value => Assert.Contains("omni-data-grid__cell--numeric", value, StringComparison.Ordinal));
        Assert.All(Classes("name"), value => Assert.DoesNotContain("omni-data-grid__cell--numeric", value, StringComparison.Ordinal));
        Assert.All(Classes("cost"), value =>
        {
            Assert.Contains("omni-data-grid__column--align-center", value, StringComparison.Ordinal);
            Assert.DoesNotContain("omni-data-grid__cell--numeric", value, StringComparison.Ordinal);
        });
        Assert.All(Classes("lambda"), value => Assert.DoesNotContain("omni-data-grid__cell--numeric", value, StringComparison.Ordinal));
        // A template lays the cell out itself (here a link from the start): header and cells keep the
        // start instead of splitting the header to the end over start-aligned content.
        var templatedHeader = grid.FindAll("thead th").Single(cell => cell.TextContent.Contains("Numéro", StringComparison.Ordinal));
        Assert.DoesNotContain("omni-data-grid__cell--numeric", templatedHeader.ClassList);
        Assert.All(Classes("templated"), value => Assert.Contains("omni-data-grid__column--align-start", value, StringComparison.Ordinal));
        Assert.Equal("end", ShippedLookTests.Value(ShippedLookTests.Body(".omni-data-grid .omni-data-grid__cell--numeric"), "text-align"));
        Assert.Equal("tabular-nums", ShippedLookTests.Value(ShippedLookTests.Body(".omni-data-grid .omni-data-grid__cell--numeric"), "font-variant-numeric"));

        var selected = grid.FindAll("tbody tr").Single(row => row.ClassList.Contains("omni-data-grid__row--selected"));
        Assert.Equal("true", selected.GetAttribute("aria-selected"));
    }

    [Fact]
    public void SystemScope_IsLightWhenTheSystemIs_AndDarkWhenItIsDark()
    {
        var css = ShippedLookTests.Uncommented(File.ReadAllText(Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "omnieurope.blazor.css")));
        Assert.Matches(@"@media \(prefers-color-scheme: light\) \{\s*\[data-omni-theme=""system""\] \{ color-scheme: light; \}", css);
        Assert.Matches(@"@media \(prefers-color-scheme: dark\) \{\s*\[data-omni-theme=""system""\] \{[^}]*color-scheme: dark;", css);

        // A preset scope following the system keeps the attribute these rules read.
        JSInterop.SetupModule("./_content/OmniEurope.Blazor/omni-theme.js");
        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.System)
            .Add(component => component.Preset, OmniThemePresets.All[0])
            .AddChildContent("Contenu"));
        Assert.Equal("system", scope.Find(".omni-theme-scope").GetAttribute("data-omni-theme"));
    }

    // ---- T17 c, T18 c: form states ----

    [Fact]
    public void Fieldsets_TakeTheWidthOfTheirContainer_AndTheLegendItsWholeLine()
    {
        foreach (var selector in new[] { ".omni-fieldset", ".omni-choice-list" })
        {
            var body = ShippedLookTests.Body(selector);
            Assert.Equal("100%", ShippedLookTests.Value(body, "inline-size"));
            Assert.Equal("0", ShippedLookTests.Value(body, "min-inline-size"));
        }

        Assert.Equal("100%", ShippedLookTests.Value(ShippedLookTests.Body(".omni-choice-list__legend"), "inline-size"));
    }

    [Fact]
    public void RadioDot_IsEightPixels_AndNoCheckBoxRuleCanReachTheSwitch()
    {
        Assert.Equal("0.5rem", ShippedLookTests.Value(ShippedLookTests.Body(".omni-radio::before"), "inline-size"));
        Assert.DoesNotContain(ShippedLookTests.Rules(), rule => rule.Selector.Contains("omni-checkbox", StringComparison.Ordinal) && rule.Selector.Contains("omni-switch", StringComparison.Ordinal));

        // The switch is a button with the switch role: no check box input a check box rule could match.
        var value = false;
        var toggle = Render<OmniSwitch>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));
        Assert.Equal("BUTTON", toggle.Find(".omni-switch").TagName);
        Assert.Equal("switch", toggle.Find(".omni-switch").GetAttribute("role"));
        Assert.Empty(toggle.FindAll("input"));
    }

    [Fact]
    public void DisabledAndReadOnlyFields_LookDistinct()
    {
        var disabled = ShippedLookTests.Body(".omni-input:disabled");
        var readOnly = ShippedLookTests.Body(".omni-input[readonly]");
        Assert.Equal("var(--omni-color-surface-muted)", ShippedLookTests.Value(disabled, "background"));
        Assert.Equal("var(--omni-color-text-muted)", ShippedLookTests.Value(disabled, "color"));
        Assert.Equal("not-allowed", ShippedLookTests.Value(disabled, "cursor"));
        Assert.Equal("transparent", ShippedLookTests.Value(readOnly, "background"));
        Assert.Equal("dashed", ShippedLookTests.Value(readOnly, "border-style"));
        Assert.DoesNotContain("opacity", disabled, StringComparison.Ordinal);
        Assert.Contains(":not([readonly])", ShippedLookTests.Rules().Single(rule => rule.Selector.StartsWith(".omni-input:hover", StringComparison.Ordinal)).Selector, StringComparison.Ordinal);
        Assert.Equal("var(--omni-color-danger)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-input[aria-invalid=\"true\"]"), "border-color"));
    }

    [Fact]
    public void FieldError_LeadsWithADecorativeGlyph()
    {
        var field = Render<OmniFormField>(parameters => parameters
            .Add(component => component.Id, "name")
            .Add(component => component.Text, "Nom")
            .Add(component => component.Error, "Obligatoire"));

        var error = field.Find(".omni-form-field__error");
        Assert.Equal("true", error.QuerySelector("svg.omni-form-field__error-icon")!.GetAttribute("aria-hidden"));
        Assert.Equal("Obligatoire", error.TextContent.Trim());
    }

    // ---- T20: settings tiles ----

    [Fact]
    public void SettingsTile_StandsOutOnSixHundredthsOfTheTextInTheCardFill()
    {
        Assert.Equal(
            "color-mix(in srgb, var(--omni-color-text) 6%, var(--omni-card-background, var(--omni-color-surface)))",
            ShippedLookTests.Value(ShippedLookTests.Body(".omni-settings-tile"), "background"));
        Assert.Equal("var(--omni-border-width) solid var(--omni-card-border-color, var(--omni-color-border))", ShippedLookTests.Value(ShippedLookTests.Body(".omni-settings-tile"), "border"));
    }

    // ---- T20, T21: the file field and the reduced list ----

    [Fact]
    public void UploadField_WeldsAReadOnlyFieldToABrowseButton_UnderOneNativeControl()
    {
        IReadOnlyList<OmniUploadFile> files = [];
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Display, OmniUploadDisplay.Field)
            .Add(component => component.InputId, "manifest")
            .Add(component => component.Files, files)
            .Add(component => component.FilesChanged, next => files = next));

        Assert.Empty(upload.FindAll(".omni-upload__zone"));
        var field = upload.Find(".omni-upload__field");
        Assert.Equal("Aucun fichier choisi", field.QuerySelector(".omni-upload__field-value")!.TextContent);
        Assert.Contains("Parcourir", field.QuerySelector(".omni-upload__field-button")!.TextContent, StringComparison.Ordinal);
        // The native control sits in the field and covers it: a click or Enter on it opens the picker.
        var input = field.QuerySelector("input[type=file]")!;
        Assert.Equal("manifest", input.Id);
        Assert.Contains("omni-upload__input", input.ClassList);
        Assert.Contains("manifest-value", input.GetAttribute("aria-describedby"), StringComparison.Ordinal);

        upload.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "deploy.yml", contentType: "text/yaml"));
        upload.Render(parameters => parameters.Add(component => component.Files, files));

        Assert.Equal("deploy.yml (5 o)", upload.Find(".omni-upload__field-value").TextContent);
        Assert.Empty(upload.FindAll(".omni-upload__list"));

        Assert.Equal("0", ShippedLookTests.Value(ShippedLookTests.Body(".omni-upload__field-value"), "border-end-end-radius"));
        Assert.Equal("0", ShippedLookTests.Value(ShippedLookTests.Body(".omni-upload__field-button"), "border-start-start-radius"));
        Assert.DoesNotMatch(@"(?:^|;)\s*gap\s*:", ShippedLookTests.Body(".omni-upload__field"));
    }

    [Fact]
    public void UploadField_KeepsTheZone_ForASelectionOfSeveralFiles()
    {
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Display, OmniUploadDisplay.Field)
            .Add(component => component.Multiple, true));

        Assert.NotNull(upload.Find(".omni-upload__zone"));
        Assert.Empty(upload.FindAll(".omni-upload__field"));
    }

    [Fact]
    public void ReducedList_ShowsTheThreeMostRecent_WithTheCountShowAllAndRemoveAll()
    {
        IReadOnlyList<OmniUploadFile> files = Enumerable.Range(1, 5).Select(index => new OmniUploadFile($"f{index}.pdf", 1024)).ToList();
        var removed = new List<string>();
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Multiple, true)
            .Add(component => component.ReducedList, true)
            .Add(component => component.Files, files)
            .Add(component => component.FilesChanged, next => files = next)
            .Add(component => component.FileRemoved, file => removed.Add(file.Name)));

        Assert.Equal(["f3.pdf", "f4.pdf", "f5.pdf"], upload.FindAll(".omni-upload__file-name").Select(name => name.TextContent));
        Assert.Equal("5 fichiers", upload.Find(".omni-upload__count").TextContent);
        var showAll = upload.Find(".omni-upload__show-all");
        Assert.Equal("false", showAll.GetAttribute("aria-expanded"));
        Assert.Equal(upload.Find(".omni-upload__list").Id, showAll.GetAttribute("aria-controls"));
        Assert.Contains("Afficher tout (5)", showAll.TextContent, StringComparison.Ordinal);

        showAll.Click();
        Assert.Equal(5, upload.FindAll(".omni-upload__file").Count);
        Assert.Equal("true", upload.Find(".omni-upload__show-all").GetAttribute("aria-expanded"));
        Assert.Contains("Réduire", upload.Find(".omni-upload__show-all").TextContent, StringComparison.Ordinal);

        upload.Find(".omni-upload__clear").Click();
        Assert.Equal(["f1.pdf", "f2.pdf", "f3.pdf", "f4.pdf", "f5.pdf"], removed);
        Assert.Empty(files);

        Assert.Equal("auto", ShippedLookTests.Value(ShippedLookTests.Body(".omni-upload__clear"), "margin-inline-start"));
        Assert.Equal("rotate(180deg)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-upload__show-all[aria-expanded=\"true\"] .omni-upload__chevron"), "transform"));
    }

    [Fact]
    public void ReducedList_HidesShowAll_AtThreeFilesOrFewer_AndDisablesRemoveAll_WhenEmpty()
    {
        IReadOnlyList<OmniUploadFile> files = [new("a.pdf", 10), new("b.pdf", 10), new("c.pdf", 10)];
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Multiple, true)
            .Add(component => component.ReducedList, true)
            .Add(component => component.Files, files)
            .Add(component => component.FilesChanged, next => files = next));

        Assert.Equal(3, upload.FindAll(".omni-upload__file").Count);
        Assert.Empty(upload.FindAll(".omni-upload__show-all"));
        Assert.Equal("3 fichiers", upload.Find(".omni-upload__count").TextContent);

        upload.Render(parameters => parameters.Add(component => component.Files, (IReadOnlyList<OmniUploadFile>)[]));
        Assert.Equal("Aucun fichier", upload.Find(".omni-upload__count").TextContent);
        Assert.True(upload.Find(".omni-upload__clear").HasAttribute("disabled"));

        var plain = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Multiple, true)
            .Add(component => component.Files, files)
            .Add(component => component.FilesChanged, next => files = next));
        Assert.Empty(plain.FindAll(".omni-upload__toolbar"));
    }

    [Fact]
    public void UploadProgress_IsTheProgressTheUploadReports_NotATimer()
    {
        // The bar shows what the upload reports while it runs, then 100 once it has succeeded; nothing
        // moves it on its own.
        string? during = null;
        IRenderedComponent<OmniUpload>? upload = null;
        upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Display, OmniUploadDisplay.Field)
            .Add(component => component.Upload, async request =>
            {
                request.ReportProgress(37);
                await Task.Yield();
                during = upload!.Markup;
            }));

        upload.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "note.txt", contentType: "text/plain"));

        Assert.Contains("aria-valuenow=\"37\"", during, StringComparison.Ordinal);
        upload.WaitForAssertion(() => Assert.Contains("aria-valuenow=\"100\"", upload.Markup, StringComparison.Ordinal));
    }

    // ---- 7B: the notification mark ----

    [Fact]
    public void NotificationMark_IsTheAlertDisc_OnTheFillAndInkOfTheSameIntention()
    {
        var mark = ShippedLookTests.Body(".omni-notification__mark");
        Assert.Equal("var(--omni-alert-icon)", ShippedLookTests.Value(mark, "block-size"));
        Assert.Equal("var(--omni-radius-circle)", ShippedLookTests.Value(mark, "border-radius"));
        Assert.Equal("var(--omni-color-info-bright)", ShippedLookTests.Value(mark, "--omni-notification-mark-fill"));
        Assert.Equal("var(--omni-color-on-bright)", ShippedLookTests.Value(mark, "--omni-notification-mark-ink"));
        Assert.Equal("var(--omni-color-success-bright)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-notification--success .omni-notification__mark"), "--omni-notification-mark-fill"));
        foreach (var (severity, fill) in new[] { ("warning", "warning"), ("error", "danger") })
        {
            var body = ShippedLookTests.Body($".omni-notification--{severity} .omni-notification__mark");
            Assert.Equal($"var(--omni-color-{fill}-deep)", ShippedLookTests.Value(body, "--omni-notification-mark-fill"));
            Assert.Equal("var(--omni-color-on-deep)", ShippedLookTests.Value(body, "--omni-notification-mark-ink"));
        }

        Assert.Equal("var(--omni-alert-glyph)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-notification__glyph"), "block-size"));
        Assert.DoesNotContain(ShippedLookTests.Rules(), rule => rule.Selector.Contains("omni-notification", StringComparison.Ordinal) && Regex.IsMatch(rule.Body, @"border-inline-start|border-left\b"));
    }

    [Theory]
    [InlineData(OmniNotificationSeverity.Information, OmniAlertSeverity.Info)]
    [InlineData(OmniNotificationSeverity.Success, OmniAlertSeverity.Success)]
    [InlineData(OmniNotificationSeverity.Warning, OmniAlertSeverity.Warning)]
    [InlineData(OmniNotificationSeverity.Error, OmniAlertSeverity.Danger)]
    public void NotificationMark_DrawsTheGlyphOfTheAlertOfTheSameSeverity(OmniNotificationSeverity notificationSeverity, OmniAlertSeverity alertSeverity)
    {
        var notification = Render<OmniNotification>(parameters => parameters
            .Add(component => component.Message, "Message")
            .Add(component => component.Severity, notificationSeverity));
        var alert = Render<OmniAlert>(parameters => parameters
            .Add(component => component.Severity, alertSeverity)
            .AddChildContent("Message"));

        Assert.Equal(
            alert.Find(".omni-alert__icon path").GetAttribute("d"),
            notification.Find(".omni-notification__mark path").GetAttribute("d"));
    }

    // ---- T18 b, T17 c: the shell ----

    [Fact]
    public void ClosedSidebar_CollapsesItsMenuToARailThatKeepsTheIcons()
    {
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, false)
            .Add(component => component.Collapse, OmniSidebarCollapse.Icons)
            .AddChildContent<OmniPanelMenu>(menu => menu
                .AddChildContent<OmniPanelMenuItem>(item => item
                    .Add(component => component.Text, "Journaux")
                    .Add(component => component.Href, "journaux")
                    .Add(component => component.Icon, (RenderFragment)(builder =>
                    {
                        builder.OpenComponent<OmniIcon>(0);
                        builder.AddAttribute(1, nameof(OmniIcon.Name), OmniIconName.Info);
                        builder.CloseComponent();
                    })))));

        Assert.Contains("omni-panel-menu--icons", sidebar.Find(".omni-panel-menu").ClassList);
        Assert.NotNull(sidebar.Find(".omni-panel-menu__link svg"));
        Assert.Equal("Journaux", sidebar.Find(".omni-panel-menu__text").TextContent.Trim());
    }

    [Fact]
    public void PanelMenu_MarksStatesByFillAndWeight_NeverByAnEdgeBar()
    {
        var current = ShippedLookTests.Body(".omni-panel-menu [aria-current=\"page\"],\n.omni-panel-menu__link--current");
        Assert.Equal("var(--omni-color-accent-subtle)", ShippedLookTests.Value(current, "background"));
        Assert.Equal("var(--omni-color-accent-strong)", ShippedLookTests.Value(current, "color"));
        Assert.Equal("600", ShippedLookTests.Value(current, "font-weight"));
        Assert.Equal("var(--omni-color-surface-hover)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-panel-menu__link:hover"), "background"));

        var rail = ShippedLookTests.Body(".omni-panel-menu--icons .omni-panel-menu__group--open > .omni-panel-menu__children");
        Assert.DoesNotContain("border-inline-start", rail, StringComparison.Ordinal);
        Assert.Contains("background", rail, StringComparison.Ordinal);
    }

    [Fact]
    public void AppBar_IsOneWrappingRow_ClosedByTheCardRule_WithAGhostBurger()
    {
        var header = ShippedLookTests.Body(".omni-header");
        Assert.Equal("flex", ShippedLookTests.Value(header, "display"));
        Assert.Equal("center", ShippedLookTests.Value(header, "align-items"));
        Assert.Equal("var(--omni-border-width) solid var(--omni-card-border-color, var(--omni-color-border))", ShippedLookTests.Value(header, "border-block-end"));

        var burger = ShippedLookTests.Body(".omni-sidebar-toggle");
        Assert.Equal("transparent", ShippedLookTests.Value(burger, "background"));
        Assert.Equal("var(--omni-control-height)", ShippedLookTests.Value(burger, "min-inline-size"));
        Assert.Equal("var(--omni-color-surface-hover)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-sidebar-toggle:hover"), "background"));

        // The account menu is a disclosure: closed at rest.
        var menu = Render<OmniProfileMenu>(parameters => parameters
            .Add(component => component.Summary, (RenderFragment)(builder => builder.AddContent(0, "AB"))));
        Assert.False(menu.Find("details.omni-profile-menu").HasAttribute("open"));
    }

    private static HashSet<string> Declared(string body) =>
        Regex.Matches(body, @"(--omni-[a-z0-9-]+)\s*:").Select(match => match.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
}
