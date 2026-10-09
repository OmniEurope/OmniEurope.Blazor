using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A legend entry that hides its series and shows it again (OmniLegend.AllowToggle, recette R-011):
/// the series, its decorations and its hover values leave the drawing, the value axis is computed
/// without them, and the entry says its state.
/// </summary>
public sealed class ChartLegendToggleTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniChartPoint> Small = [new(0, 10), new(1, 20)];
    private static readonly IReadOnlyList<OmniChartPoint> Large = [new(0, 400), new(1, 500)];

    private IRenderedComponent<OmniChart> Chart(OmniLegendPosition position, bool allowToggle = true) =>
        Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Soldes")
            .Add(component => component.SharedTooltip, true)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniValueAxis>(0);
                builder.AddAttribute(1, nameof(OmniValueAxis.Automatic), true);
                builder.CloseComponent();
                builder.OpenComponent<OmniLineSeries>(2);
                builder.AddAttribute(3, nameof(OmniLineSeries.Title), "Livret");
                builder.AddAttribute(4, nameof(OmniLineSeries.ColorIndex), 0);
                builder.AddAttribute(5, nameof(OmniLineSeries.Data), Small);
                builder.CloseComponent();
                builder.OpenComponent<OmniLineSeries>(6);
                builder.AddAttribute(7, nameof(OmniLineSeries.Title), "Total");
                builder.AddAttribute(8, nameof(OmniLineSeries.ColorIndex), 1);
                builder.AddAttribute(9, nameof(OmniLineSeries.Data), Large);
                builder.CloseComponent();
                builder.OpenComponent<OmniMarkers>(10);
                builder.AddAttribute(11, nameof(OmniMarkers.ColorIndex), 1);
                builder.AddAttribute(12, nameof(OmniMarkers.Data), Large);
                builder.CloseComponent();
                builder.OpenComponent<OmniSeriesDataLabels>(13);
                builder.AddAttribute(14, nameof(OmniSeriesDataLabels.Data), Large);
                builder.CloseComponent();
                builder.OpenComponent<OmniLegend>(15);
                builder.AddAttribute(16, nameof(OmniLegend.Position), position);
                builder.AddAttribute(17, nameof(OmniLegend.AllowToggle), allowToggle);
                builder.CloseComponent();
            }));

    private static double TopTick(IRenderedComponent<OmniChart> chart) =>
        chart.FindAll(".omni-chart__axis--value text")
            .Select(text => double.Parse(text.TextContent.Replace(" ", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal), NumberStyles.Number, CultureInfo.CurrentCulture))
            .Max();

    [Fact]
    public void ABottomEntry_IsAPressedButton_ThatHidesItsSeriesAndItsDecorations_ThenShowsThemAgain()
    {
        var chart = Chart(OmniLegendPosition.Bottom);
        chart.WaitForAssertion(() => Assert.Equal(2, chart.FindAll("button.omni-chart__legend-toggle").Count));
        var total = chart.FindAll("button.omni-chart__legend-toggle")[1];
        Assert.Equal("true", total.GetAttribute("aria-pressed"));
        Assert.Equal(2, chart.FindAll("polyline").Count);
        Assert.Single(chart.FindAll(".omni-chart__markers"));
        Assert.Single(chart.FindAll(".omni-chart__labels"));
        Assert.True(TopTick(chart) >= 500);

        total.Click();

        chart.WaitForAssertion(() =>
        {
            Assert.Single(chart.FindAll("polyline"));
            Assert.Empty(chart.FindAll(".omni-chart__markers"));
            Assert.Empty(chart.FindAll(".omni-chart__labels"));
            var entry = chart.FindAll("button.omni-chart__legend-toggle")[1];
            Assert.Equal("false", entry.GetAttribute("aria-pressed"));
            Assert.Contains("omni-chart__legend-item--hidden", entry.ParentElement!.ClassList);
            Assert.True(TopTick(chart) < 100);
            Assert.All(chart.FindAll(".omni-chart__hover-band title"), title => Assert.DoesNotContain("Total", title.TextContent, StringComparison.Ordinal));
        });

        chart.FindAll("button.omni-chart__legend-toggle")[1].Click();

        chart.WaitForAssertion(() =>
        {
            Assert.Equal(2, chart.FindAll("polyline").Count);
            Assert.Single(chart.FindAll(".omni-chart__markers"));
            Assert.Equal("true", chart.FindAll("button.omni-chart__legend-toggle")[1].GetAttribute("aria-pressed"));
        });
    }

    [Fact]
    public void ARightEntry_AnswersThePointer_InsideTheDrawing()
    {
        var chart = Chart(OmniLegendPosition.Right);
        chart.WaitForAssertion(() => Assert.Equal(2, chart.FindAll("g.omni-chart__legend-entry--toggle").Count));
        // A button of the keyboard too (audit of 2026-10-07, RCL-003): one tab stop each, pressed while
        // its series shows, and marked for the page keys that turn Enter and Space into a click.
        Assert.All(chart.FindAll("g.omni-chart__legend-entry--toggle"), entry =>
        {
            Assert.Equal("button", entry.GetAttribute("role"));
            Assert.Equal("0", entry.GetAttribute("tabindex"));
            Assert.Equal("true", entry.GetAttribute("aria-pressed"));
            Assert.True(entry.HasAttribute("data-omni-key-button"));
        });
        Assert.Equal("Livret", chart.FindAll("g.omni-chart__legend-entry--toggle")[0].GetAttribute("aria-label"));

        chart.FindAll("g.omni-chart__legend-entry--toggle")[0].Click();

        chart.WaitForAssertion(() =>
        {
            Assert.Single(chart.FindAll("polyline"));
            Assert.Contains("omni-chart__legend-entry--hidden", chart.FindAll("g.omni-chart__legend-entry")[0].ClassList);
            Assert.Equal("false", chart.FindAll("g.omni-chart__legend-entry")[0].GetAttribute("aria-pressed"));
        });
    }

    [Fact]
    public void WithoutToggle_TheLegendStaysAPlainKey()
    {
        var chart = Chart(OmniLegendPosition.Bottom, allowToggle: false);

        chart.WaitForAssertion(() => Assert.Equal(2, chart.FindAll("ul.omni-chart__legend--below li").Count));
        Assert.Empty(chart.FindAll("button.omni-chart__legend-toggle"));
    }

    [Fact]
    public void ATopEntry_HidesItsSeries_AndSaysItIsNoLongerPressed()
    {
        var chart = Chart(OmniLegendPosition.Top);
        chart.WaitForAssertion(() => Assert.Equal(2, chart.FindAll("ul.omni-chart__legend--above button.omni-chart__legend-toggle").Count));

        chart.FindAll("button.omni-chart__legend-toggle")[0].Click();

        chart.WaitForAssertion(() =>
        {
            Assert.Single(chart.FindAll("polyline"));
            Assert.Equal("false", chart.FindAll("button.omni-chart__legend-toggle")[0].GetAttribute("aria-pressed"));
            Assert.Equal("true", chart.FindAll("button.omni-chart__legend-toggle")[1].GetAttribute("aria-pressed"));
        });
    }

    [Fact]
    public void ATopLegendWithoutToggle_IsAPlainKeyAboveTheDrawing()
    {
        var chart = Chart(OmniLegendPosition.Top, allowToggle: false);

        chart.WaitForAssertion(() => Assert.Equal(2, chart.FindAll("ul.omni-chart__legend--above li").Count));
        Assert.Empty(chart.FindAll("button.omni-chart__legend-toggle"));
        Assert.All(chart.FindAll("ul.omni-chart__legend--above li"), item => Assert.NotNull(item.QuerySelector("span.omni-chart__swatch")));
    }

    [Fact]
    public void AnAreaAndAColumnSeries_LeaveTheDrawing_WhenTheirEntryHidesThem()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Stock")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniValueAxis>(0);
                builder.AddAttribute(1, nameof(OmniValueAxis.Automatic), true);
                builder.CloseComponent();
                builder.OpenComponent<OmniAreaSeries>(2);
                builder.AddAttribute(3, nameof(OmniAreaSeries.Title), "Entrées");
                builder.AddAttribute(4, nameof(OmniAreaSeries.ColorIndex), 0);
                builder.AddAttribute(5, nameof(OmniAreaSeries.Data), Small);
                builder.CloseComponent();
                builder.OpenComponent<OmniColumnSeries>(6);
                builder.AddAttribute(7, nameof(OmniColumnSeries.Title), "Sorties");
                builder.AddAttribute(8, nameof(OmniColumnSeries.ColorIndex), 1);
                builder.AddAttribute(9, nameof(OmniColumnSeries.Data), Large);
                builder.CloseComponent();
                builder.OpenComponent<OmniLegend>(10);
                builder.AddAttribute(11, nameof(OmniLegend.Position), OmniLegendPosition.Bottom);
                builder.CloseComponent();
            }));
        chart.WaitForAssertion(() => Assert.Equal(2, chart.FindAll("button.omni-chart__legend-toggle").Count));
        Assert.Single(chart.FindAll("polygon.omni-chart__area"));
        Assert.Single(chart.FindAll("g.omni-chart__columns"));

        chart.FindAll("button.omni-chart__legend-toggle")[0].Click();
        chart.FindAll("button.omni-chart__legend-toggle")[1].Click();

        chart.WaitForAssertion(() =>
        {
            Assert.Empty(chart.FindAll("polygon.omni-chart__area"));
            Assert.Empty(chart.FindAll("g.omni-chart__columns"));
            Assert.All(chart.FindAll("button.omni-chart__legend-toggle"), button => Assert.Equal("false", button.GetAttribute("aria-pressed")));
        });
    }

    [Theory]
    [InlineData(OmniLegendPosition.Bottom, "below")]
    [InlineData(OmniLegendPosition.Top, "above")]
    public void PieSlices_NeverToggle(OmniLegendPosition position, string side)
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Parts")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniPieSeries>(0);
                builder.AddAttribute(1, nameof(OmniPieSeries.Data), (IReadOnlyList<OmniChartSlice>)[new("A", 1), new("B", 2)]);
                builder.CloseComponent();
                builder.OpenComponent<OmniLegend>(2);
                builder.AddAttribute(3, nameof(OmniLegend.Position), position);
                builder.CloseComponent();
            }));

        chart.WaitForAssertion(() => Assert.Equal(2, chart.FindAll($"ul.omni-chart__legend--{side} li").Count));
        Assert.Empty(chart.FindAll("button.omni-chart__legend-toggle"));
    }
}
