using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The control size setting, the scrollable tab panels, the grid's wheel scope, the one-condition
/// filter popover and the hover every settings tile answers with.
/// </summary>
public sealed class ControlScaleAndScrollOptionsTests : OmniBunitContext
{
    private sealed record Row(int Number, string Name);

    // ---- control size ----

    [Theory]
    [InlineData(1, 0.75)]
    [InlineData(5, 1.0)]
    [InlineData(10, 1.3125)]
    public void Control_size_level_sets_the_control_scale_on_the_root(int level, double scale)
    {
        var body = ShippedLookTests.Body($"html[data-oe-control-size=\"{level}\"]");

        Assert.Equal(scale, double.Parse(ShippedLookTests.Value(body, "--omni-control-scale"), CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Control_size_setting_is_hidden_until_the_host_binds_it()
    {
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.TextSizeLevelChanged, _ => { })
            .Add(component => component.DensityLevelChanged, _ => { }));

        settings.FindAll(".omni-appearance-settings__row")[4].QuerySelector("button")!.Click();

        Assert.Equal(2, settings.FindAll(".omni-appearance-settings--scale input[type=range]").Count);
        Assert.DoesNotContain("Taille des contrôles", settings.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Bound_control_size_gets_its_steps_its_slider_and_its_default()
    {
        int? controlSize = null;
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.ControlSizeLevel, 8)
            .Add(component => component.ControlSizeLevelChanged, value => controlSize = value));

        Assert.Contains("Taille du texte / Densité / Contrôles", settings.Markup, StringComparison.Ordinal);
        settings.FindAll(".omni-appearance-settings__row")[4].QuerySelector("button")!.Click();
        var rows = settings.FindAll(".omni-appearance-settings--scale .omni-appearance-settings__row");
        Assert.Equal(3, rows.Count);
        Assert.Contains("Taille des contrôles", rows[2].TextContent, StringComparison.Ordinal);
        Assert.Contains("8/10", rows[2].TextContent, StringComparison.Ordinal);

        settings.FindAll(".omni-appearance-settings--scale input[type=range]")[2].Input("3");
        Assert.Equal(3, controlSize);

        settings.FindAll(".omni-appearance-settings--scale .omni-appearance-settings__row")[2].QuerySelectorAll("button").Last().Click();
        Assert.Equal(5, controlSize);
    }

    // ---- tooltip ----

    [Fact]
    public void Tooltip_waits_300_ms_by_default_and_points_at_what_it_describes()
    {
        var delay = ShippedLookTests.Rules().First(rule => rule.Selector == ".omni-tooltip" && rule.Body.Contains("--omni-tooltip-delay", StringComparison.Ordinal));
        Assert.Equal("300ms", ShippedLookTests.Value(delay.Body, "--omni-tooltip-delay"));
        var arrow = ShippedLookTests.Body(".omni-tooltip__content::after");
        Assert.Contains("var(--omni-tooltip-arrow, 50%)", arrow, StringComparison.Ordinal);
        Assert.Contains("border-bottom-color", ShippedLookTests.Body(".omni-tooltip--below .omni-tooltip__content::after"), StringComparison.Ordinal);
    }

    // ---- tiles ----

    [Fact]
    public void Every_settings_tile_and_appearance_row_answers_the_pointer()
    {
        Assert.Contains("background", ShippedLookTests.Body(".omni-settings-tile:hover"), StringComparison.Ordinal);
        Assert.Contains("background", ShippedLookTests.Body(".omni-appearance-settings__row:hover"), StringComparison.Ordinal);
    }

    // ---- tabs ----

    [Fact]
    public void Scrollable_panels_are_opt_in()
    {
        RenderFragment tabs = builder =>
        {
            builder.OpenComponent<OmniTabsItem>(0);
            builder.AddAttribute(1, nameof(OmniTabsItem.Title), "One");
            builder.AddAttribute(2, nameof(OmniTabsItem.ChildContent), (RenderFragment)(content => content.AddContent(0, "Panel")));
            builder.CloseComponent();
        };

        var plain = Render<OmniTabs>(parameters => parameters.Add(component => component.Tabs, tabs));
        var scrolling = Render<OmniTabs>(parameters => parameters
            .Add(component => component.Tabs, tabs)
            .Add(component => component.ScrollablePanels, true));

        Assert.DoesNotContain("omni-tabs--scrollable-panels", plain.Markup, StringComparison.Ordinal);
        Assert.Contains("omni-tabs--scrollable-panels", scrolling.Find(".omni-tabs").ClassName, StringComparison.Ordinal);
        Assert.Equal("auto", ShippedLookTests.Value(ShippedLookTests.Body(".omni-tabs--scrollable-panels > .omni-tabs__panel"), "overflow-y"));
    }

    // ---- grid wheel scope ----

    [Fact]
    public void Wheel_scope_is_handed_to_the_grid_script_only_when_named()
    {
        var module = JSInterop.SetupModule("./_content/OmniEurope.Blazor/omni-grid.js");

        Render<OmniDataGrid<int>>(parameters => parameters.Add(component => component.Items, new[] { 1, 2, 3 }));
        Assert.Empty(module.Invocations["attachWheelScope"]);

        Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, new[] { 1, 2, 3 })
            .Add(component => component.WheelScrollScope, " .page-content "));

        var invocation = Assert.Single(module.Invocations["attachWheelScope"]);
        Assert.Equal(".page-content", invocation.Arguments[1]);
    }

    // ---- one-condition filter popover ----

    [Fact]
    public void One_condition_popover_puts_the_value_and_a_square_clear_under_the_operator()
    {
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, new[] { new Row(1, "Alpha"), new Row(12, "Beta"), new Row(28, "Gamma") })
            .Add(component => component.AllowFiltering, true)
            .Add(component => component.FilterMode, OmniDataGridFilterMode.SimpleWithMenu)
            .Add(component => component.ShowHeaderFilterMenu, true)
            .Add(component => component.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniDataGridColumn<Row>>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Key), "number");
                builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "N°");
                builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.Value), (Func<Row, object?>)(row => row.Number));
                builder.AddComponentParameter(4, nameof(OmniDataGridColumn<Row>.Filterable), true);
                builder.AddComponentParameter(5, nameof(OmniDataGridColumn<Row>.FilterType), OmniDataGridColumnFilterType.Number);
                builder.CloseComponent();
            })));

        var panel = grid.Find(".omni-data-grid__filter-menu-panel");
        var editor = panel.QuerySelector(".omni-data-grid__filter-editor")!;
        Assert.Equal("SELECT", editor.Children[1].TagName);
        var entry = editor.QuerySelector(".omni-data-grid__filter-entry")!;
        Assert.NotNull(entry.QuerySelector("input.omni-data-grid__filter"));
        var clear = entry.QuerySelector("button.omni-data-grid__filter-clear--icon")!;
        Assert.Equal("Effacer", clear.GetAttribute("aria-label"));
        Assert.Equal(string.Empty, clear.TextContent.Trim());
        Assert.Empty(panel.QuerySelectorAll(".omni-data-grid__filter-actions"));

        grid.Find(".omni-data-grid__filter-menu-panel input.omni-data-grid__filter").Input("2");
        Assert.Equal(2, grid.FindAll("tbody tr").Count);
        grid.Find(".omni-data-grid__filter-menu-panel .omni-data-grid__filter-clear--icon").Click();
        Assert.Equal(3, grid.FindAll("tbody tr").Count);
    }
}
