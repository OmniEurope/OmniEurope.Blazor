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
        Assert.Empty(chart.FindAll("g.omni-chart__legend-entry--toggle[tabindex]"));

        chart.FindAll("g.omni-chart__legend-entry--toggle")[0].Click();

        chart.WaitForAssertion(() =>
        {
            Assert.Single(chart.FindAll("polyline"));
            Assert.Contains("omni-chart__legend-entry--hidden", chart.FindAll("g.omni-chart__legend-entry")[0].ClassList);
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
    public void PieSlices_NeverToggle()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Parts")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniPieSeries>(0);
                builder.AddAttribute(1, nameof(OmniPieSeries.Data), (IReadOnlyList<OmniChartSlice>)[new("A", 1), new("B", 2)]);
                builder.CloseComponent();
                builder.OpenComponent<OmniLegend>(2);
                builder.AddAttribute(3, nameof(OmniLegend.Position), OmniLegendPosition.Bottom);
                builder.CloseComponent();
            }));

        chart.WaitForAssertion(() => Assert.Equal(2, chart.FindAll("ul.omni-chart__legend--below li").Count));
        Assert.Empty(chart.FindAll("button.omni-chart__legend-toggle"));
    }
}
