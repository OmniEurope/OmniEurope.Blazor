using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The recurring requests of the applications on the default look (PLAN-001 lot 5): stat tiles of one
/// row as tall as each other, a chart never taller than the window, an icon-only button square whatever
/// names it, and the line numbers of a log that stay in place when it scrolls sideways. What only a
/// browser measures (the heights, the scroll) is left to the showcase.
/// </summary>
public sealed class DefaultLookRequestsTests : OmniBunitContext
{
    private static readonly RenderFragment LoneIcon = builder =>
    {
        builder.OpenComponent<OmniIcon>(0);
        builder.AddComponentParameter(1, nameof(OmniIcon.Name), OmniIconName.Delete);
        builder.CloseComponent();
    };

    // ---- G-D23-03: stat tiles of a row share the tallest height ----

    [Fact]
    public void A_stat_tile_fills_the_height_of_its_column_like_a_navigation_tile()
    {
        var tile = ShippedLookTests.Body(".omni-stat-tile");

        Assert.Equal("100%", ShippedLookTests.Value(tile, "block-size"));
        // Border and padding count inside that height, so a stretched tile does not overflow its column.
        Assert.Contains(ShippedLookTests.Rules(), rule => rule.Selector.Contains(".omni-stat-tile", StringComparison.Ordinal) && rule.Body.Contains("box-sizing: border-box", StringComparison.Ordinal));
    }

    // ---- G-D35-02: a default height cap and MaxHeight ----

    [Fact]
    public void A_chart_drawing_is_capped_in_height_by_default_and_keeps_its_proportions()
    {
        var svg = ShippedLookTests.Body(".omni-chart__svg");
        Assert.Equal("var(--omni-chart-max-height, min(32rem, 70vh))", ShippedLookTests.Value(svg, "max-height"));

        var chart = Render<OmniChart>(parameters => parameters.Add(component => component.Label, "Soldes"));
        // Capped, the drawing is fitted whole in the middle of its box, never stretched.
        Assert.Equal("xMidYMid meet", chart.Find("svg.omni-chart__svg").GetAttribute("preserveAspectRatio"));
        Assert.Null(chart.Instance.MaxHeight);
        Assert.DoesNotContain(JSInterop.Invocations, invocation => invocation.Identifier == "import");
    }

    [Theory]
    [InlineData("20rem")]
    [InlineData("480px")]
    [InlineData("60vh")]
    [InlineData("none")]
    public void A_chart_MaxHeight_is_pushed_as_a_custom_property_by_the_interop_script(string maxHeight)
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Interop);

        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Label, "Soldes")
            .Add(component => component.MaxHeight, maxHeight));

        chart.WaitForAssertion(() => Assert.Single(module.Invocations["applyChartMaxHeight"]));
        Assert.Equal(maxHeight, module.Invocations["applyChartMaxHeight"][0].Arguments[1]);
    }

    [Fact]
    public void A_chart_MaxHeight_removed_gives_the_default_cap_back_and_is_not_pushed_again_unchanged()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Interop);
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Label, "Soldes")
            .Add(component => component.MaxHeight, "20rem"));
        chart.WaitForAssertion(() => Assert.Single(module.Invocations["applyChartMaxHeight"]));

        chart.Render(parameters => parameters.Add(component => component.Description, "Par mois"));
        Assert.Single(module.Invocations["applyChartMaxHeight"]);

        chart.Render(parameters => parameters.Add(component => component.MaxHeight, (string?)null));
        chart.WaitForAssertion(() => Assert.Equal(2, module.Invocations["applyChartMaxHeight"].Count));
        Assert.Null(module.Invocations["applyChartMaxHeight"][1].Arguments[1]);
    }

    [Theory]
    [InlineData("20")]
    [InlineData("20rem; color: red")]
    [InlineData("calc(100vh - 2rem)")]
    public void A_chart_refuses_a_MaxHeight_that_is_not_a_plain_length(string maxHeight)
    {
        var exception = Assert.Throws<ArgumentException>(() => Render<OmniChart>(parameters => parameters
            .Add(component => component.Label, "Soldes")
            .Add(component => component.MaxHeight, maxHeight)));

        Assert.Equal(nameof(OmniChart.MaxHeight), exception.ParamName);
    }

    [Fact]
    public async Task A_chart_on_a_lost_circuit_neither_sizes_nor_fails_and_still_releases_its_script()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("gone") } };
        runtime.Module.CallFailures["applyChartMaxHeight"] = new JSDisconnectedException("gone");
        Services.AddSingleton<IJSRuntime>(runtime);
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Label, "Soldes")
            .Add(component => component.MaxHeight, "20rem"));
        chart.WaitForAssertion(() => Assert.Equal(["applyChartMaxHeight"], runtime.Module.Calls));

        await chart.Instance.DisposeAsync();

        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public async Task A_chart_that_never_set_MaxHeight_loads_and_releases_no_script()
    {
        var runtime = new ManualJSRuntime();
        Services.AddSingleton<IJSRuntime>(runtime);
        var chart = Render<OmniChart>(parameters => parameters.Add(component => component.Label, "Soldes"));

        await chart.Instance.DisposeAsync();

        Assert.Empty(runtime.Module.Calls);
        Assert.False(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public void The_interop_script_writes_the_chart_cap_through_the_CSSOM()
    {
        var script = File.ReadAllText(Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "omniInterop.js"));

        Assert.Contains("export function applyChartMaxHeight(figure, maxHeight)", script, StringComparison.Ordinal);
        Assert.Contains("style.setProperty('--omni-chart-max-height'", script, StringComparison.Ordinal);
        Assert.Contains("style.removeProperty('--omni-chart-max-height')", script, StringComparison.Ordinal);
    }

    // ---- G-D25-03: an icon-only button is square whatever names it ----

    [Fact]
    public void An_icon_only_button_named_by_its_title_is_square_and_named_by_that_title()
    {
        var button = Render<OmniButton>(parameters => parameters
            .AddUnmatched("title", "Supprimer")
            .Add(component => component.ChildContent, LoneIcon));

        var element = button.Find("button");
        Assert.Contains("omni-button--icon", element.ClassList);
        Assert.Equal("Supprimer", element.GetAttribute("aria-label"));
        Assert.Equal("Supprimer", element.GetAttribute("title"));

        var square = ShippedLookTests.Body(".omni-button--icon");
        Assert.Equal("var(--omni-button-size, var(--omni-control-height))", ShippedLookTests.Value(square, "min-inline-size"));
        Assert.Equal("0", ShippedLookTests.Value(square, "padding-inline"));
    }

    [Fact]
    public void An_icon_only_button_keeps_its_Label_over_its_title_and_is_square_without_any_name()
    {
        var labelled = Render<OmniButton>(parameters => parameters
            .AddUnmatched("title", "Info-bulle")
            .Add(component => component.Label, "Supprimer la ligne")
            .Add(component => component.ChildContent, LoneIcon));
        var bare = Render<OmniButton>(parameters => parameters.Add(component => component.ChildContent, LoneIcon));

        Assert.Equal("Supprimer la ligne", labelled.Find("button").GetAttribute("aria-label"));
        Assert.Contains("omni-button--icon", bare.Find("button").ClassList);
        Assert.False(bare.Find("button").HasAttribute("aria-label"));
    }

    [Fact]
    public void A_button_with_text_is_never_squared_nor_renamed_by_its_title()
    {
        var withIcon = Render<OmniButton>(parameters => parameters
            .AddUnmatched("title", "Supprimer la sélection")
            .Add(component => component.ChildContent, (RenderFragment)(builder =>
            {
                builder.AddContent(0, LoneIcon);
                builder.AddContent(1, "Supprimer");
            })));
        var textOnly = Render<OmniButton>(parameters => parameters
            .AddUnmatched("title", "Enregistrer le brouillon")
            .AddChildContent("Enregistrer"));
        var markup = Render<OmniButton>(parameters => parameters.AddChildContent("<b>Gras</b>"));

        foreach (var element in new[] { withIcon.Find("button"), textOnly.Find("button"), markup.Find("button") })
        {
            Assert.DoesNotContain("omni-button--icon", element.ClassList);
            Assert.False(element.HasAttribute("aria-label"));
        }
    }

    [Fact]
    public void A_lone_icon_wrapped_in_whitespace_is_still_icon_only()
    {
        var button = Render<OmniButton>(parameters => parameters.Add(component => component.ChildContent, (RenderFragment)(builder =>
        {
            builder.AddMarkupContent(0, "\n    ");
            builder.AddContent(1, LoneIcon);
            builder.AddContent(2, "  ");
        })));

        Assert.Contains("omni-button--icon", button.Find("button").ClassList);
    }

    [Fact]
    public void An_icon_only_button_without_a_usable_title_gets_no_invented_name()
    {
        var untitled = Render<OmniButton>(parameters => parameters
            .AddUnmatched("data-probe", "kept")
            .Add(component => component.ChildContent, LoneIcon));
        var blank = Render<OmniButton>(parameters => parameters
            .AddUnmatched("title", "   ")
            .Add(component => component.ChildContent, LoneIcon));
        var nullTitle = Render<OmniButton>(parameters => parameters
            .AddUnmatched("title", null!)
            .Add(component => component.ChildContent, LoneIcon));
        var empty = Render<OmniButton>();

        foreach (var button in new[] { untitled, blank, nullTitle })
        {
            Assert.Contains("omni-button--icon", button.Find("button").ClassList);
            Assert.False(button.Find("button").HasAttribute("aria-label"));
        }

        Assert.DoesNotContain("omni-button--icon", empty.Find("button").ClassList);
    }

    // ---- G-D26-05: line number and time stay at the start of the log ----

    [Fact]
    public void The_number_and_the_time_of_a_log_line_share_a_gutter_that_sticks_to_the_start()
    {
        var viewer = Render<OmniLogViewer>(parameters => parameters
            .Add(component => component.Lines, [new OmniLogLine("Démarrage", OmniLogLevel.Information, new DateTimeOffset(2026, 10, 12, 8, 30, 0, TimeSpan.Zero))])
            .Add(component => component.TimeZone, TimeZoneInfo.Utc));

        var gutter = viewer.Find(".omni-log-viewer__line > .omni-log-viewer__gutter");
        Assert.NotNull(gutter.QuerySelector(".omni-log-viewer__number"));
        Assert.NotNull(gutter.QuerySelector("time.omni-log-viewer__time"));

        var css = ShippedLookTests.Body(".omni-log-viewer__gutter");
        Assert.Equal("sticky", ShippedLookTests.Value(css, "position"));
        Assert.Equal("0", ShippedLookTests.Value(css, "inset-inline-start"));
        // Opaque: the tint of the row laid over the surface of the log, so no scrolled text shows through.
        Assert.Equal("linear-gradient(var(--omni-log-row-tint, transparent), var(--omni-log-row-tint, transparent)), var(--omni-log-viewer-surface)", ShippedLookTests.Value(css, "background"));
    }

    [Theory]
    [InlineData(".omni-log-viewer__line--warning", "var(--omni-color-warning-subtle)")]
    [InlineData(".omni-log-viewer__line--error", "var(--omni-color-danger-subtle)")]
    [InlineData(".omni-log-viewer__line--current", "color-mix(in srgb, var(--omni-color-accent) 14%, transparent)")]
    public void Every_tinted_log_row_gives_its_gutter_the_same_tint(string selector, string tint)
    {
        var row = ShippedLookTests.Body(selector);

        Assert.Equal(tint, ShippedLookTests.Value(row, "--omni-log-row-tint"));
        Assert.Equal("var(--omni-log-row-tint)", ShippedLookTests.Value(row, "background"));
    }

    [Fact]
    public void A_log_without_numbers_nor_times_draws_no_gutter()
    {
        var viewer = Render<OmniLogViewer>(parameters => parameters
            .Add(component => component.Lines, [new OmniLogLine("Démarrage", OmniLogLevel.Information, null)])
            .Add(component => component.ShowLineNumbers, false));

        Assert.Empty(viewer.FindAll(".omni-log-viewer__gutter"));
    }

    [Theory]
    [InlineData(true, false, true, ".omni-log-viewer__number")]
    [InlineData(false, true, true, "time.omni-log-viewer__time")]
    [InlineData(true, true, false, ".omni-log-viewer__number")]
    public void The_gutter_holds_only_what_the_log_shows(bool numbers, bool timestamps, bool hasTime, string shown)
    {
        var timestamp = hasTime ? new DateTimeOffset(2026, 10, 12, 8, 30, 0, TimeSpan.Zero) : (DateTimeOffset?)null;
        var viewer = Render<OmniLogViewer>(parameters => parameters
            .Add(component => component.Lines, [new OmniLogLine("Démarrage", OmniLogLevel.Information, timestamp)])
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.ShowLineNumbers, numbers)
            .Add(component => component.ShowTimestamps, timestamps));

        var gutter = viewer.Find(".omni-log-viewer__gutter");
        Assert.Single(gutter.Children);
        Assert.NotNull(gutter.QuerySelector(shown));
    }

    [Fact]
    public void A_log_with_its_times_hidden_and_no_numbers_draws_no_gutter()
    {
        var viewer = Render<OmniLogViewer>(parameters => parameters
            .Add(component => component.Lines, [new OmniLogLine("Démarrage", OmniLogLevel.Information, new DateTimeOffset(2026, 10, 12, 8, 30, 0, TimeSpan.Zero))])
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.ShowLineNumbers, false)
            .Add(component => component.ShowTimestamps, false));

        Assert.Empty(viewer.FindAll(".omni-log-viewer__gutter"));
    }
}
