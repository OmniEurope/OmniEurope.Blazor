using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The chart parts in the shapes their suites leave out: an axis title, a category axis, a legend and
/// markers drawn outside a chart, grid lines and data labels of horizontal bars, a gauge scale without
/// room, a value axis whose bounds are reversed, and a shared hover text over series of unequal length.
/// </summary>
public sealed class ChartComponentEdgeTests : OmniBunitContext
{
    private static OmniChartPoint[] Points(params double[] values) => [.. values.Select((value, index) => new OmniChartPoint(index, value))];

    private static void Child<TComponent>(RenderTreeBuilder builder, int sequence, params (string Name, object? Value)[] parameters)
        where TComponent : IComponent
    {
        builder.OpenComponent<TComponent>(sequence);
        for (var index = 0; index < parameters.Length; index++)
        {
            builder.AddComponentParameter(sequence + index + 1, parameters[index].Name, parameters[index].Value);
        }

        builder.CloseComponent();
    }

    [Fact]
    public void AxisTitle_OutsideAChart_SitsOnTheDefaultPlot()
    {
        var below = Render<OmniAxisTitle>(parameters => parameters.Add(component => component.Text, "Mois"));
        var upwards = Render<OmniAxisTitle>(parameters => parameters.Add(component => component.Text, "Euros").Add(component => component.Vertical, true));

        Assert.Equal("55", below.Find("text").GetAttribute("x"));
        Assert.Equal("3", upwards.Find("text").GetAttribute("x"));
        Assert.Equal("rotate(-90 3 45)", upwards.Find("text").GetAttribute("transform"));
    }

    [Fact]
    public void CategoryAxis_OutsideAChart_KeepsItsOwnLayoutAcrossRenders_AndLeavesNothingOnDisposal()
    {
        var axis = Render<OmniCategoryAxis>(parameters => parameters.Add(component => component.Labels, ["janv.", "févr."]));
        axis.Render(parameters => parameters.Add(component => component.Labels, ["janv.", "févr.", "mars"]));

        Assert.Contains("mars", axis.Markup, StringComparison.Ordinal);
        axis.Instance.Dispose();
    }

    [Fact]
    public void Legend_OutsideAChart_TakesTheDefaultColumn()
    {
        var legend = Render<OmniLegend>(parameters => parameters
            .Add(component => component.Items, ["Ventes"])
            .Add(component => component.Position, OmniLegendPosition.Right));

        Assert.Contains("Ventes", legend.Markup, StringComparison.Ordinal);
        Assert.Contains("x=\"79", legend.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Markers_OutsideAChart_TitleEachPointByItsLabelOrItsValue()
    {
        var markers = Render<OmniMarkers>(parameters => parameters.Add(component => component.Data, [new OmniChartPoint(0, 4, "Lundi"), new OmniChartPoint(1, 7)]));
        var columns = Render<OmniColumnSeries>(parameters => parameters.Add(component => component.Data, [new OmniChartPoint(0, 4, "Lundi"), new OmniChartPoint(1, 7)]));

        Assert.Equal(["Lundi", "7"], markers.FindAll("title").Select(title => title.TextContent));
        Assert.Equal(["Lundi", "7"], columns.FindAll("title").Select(title => title.TextContent));
    }

    [Fact]
    public void HorizontalBars_DrawVerticalGridLines_AndTheirLabelsPastTheBarEnds()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Barres")
            .AddChildContent(builder =>
            {
                Child<OmniGridLines>(builder, 0, ("Count", 4));
                Child<OmniColumnSeries>(builder, 10, ("Data", Points(3, 5)), ("Horizontal", true), ("Title", "Ventes"));
                Child<OmniSeriesDataLabels>(builder, 20, ("Data", new[] { new OmniChartPoint(0, 3, "trois"), new OmniChartPoint(1, 5) }), ("FormatValue", (Func<double, string>)(value => $"{value} €")));
            }));

        chart.WaitForAssertion(() =>
        {
            var lines = chart.FindAll(".omni-chart__grid-lines line");
            Assert.Equal(4, lines.Count);
            Assert.All(lines, line => Assert.Equal(line.GetAttribute("x1"), line.GetAttribute("x2")));
            var labels = chart.FindAll(".omni-chart__labels text");
            Assert.Equal(["trois", "5 €"], labels.Select(label => label.TextContent));
            Assert.All(labels, label => Assert.Equal("central", label.GetAttribute("dominant-baseline")));
        });
    }

    [Fact]
    public void ColorByPoint_PaintsEachBar_AndInsideLabelsStartAtTheBarStart()
    {
        var data = new[] { new OmniChartPoint(0, 80, "Opus · 80%"), new OmniChartPoint(1, 20, "Sonnet · 20%") };
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Modèles")
            .AddChildContent(builder =>
            {
                Child<OmniColumnSeries>(builder, 0, ("Data", data), ("Horizontal", true), ("ColorByPoint", true), ("ColorIndex", 2));
                Child<OmniSeriesDataLabels>(builder, 10, ("Data", data), ("Inside", true));
            }));

        chart.WaitForAssertion(() =>
        {
            var bars = chart.FindAll(".omni-chart__bars rect");
            Assert.Equal(["omni-chart-color-2", "omni-chart-color-3"], bars.Select(bar => bar.GetAttribute("class")));
            Assert.NotNull(chart.Find(".omni-chart__labels.omni-chart__labels--inside"));
            var labels = chart.FindAll(".omni-chart__labels text");
            Assert.Equal(["Opus · 80%", "Sonnet · 20%"], labels.Select(label => label.TextContent));
            // Both labels begin at the start of the bars, whatever their length.
            var barStart = double.Parse(bars[0].GetAttribute("x")!, System.Globalization.CultureInfo.InvariantCulture);
            Assert.All(labels, label => Assert.Equal(barStart + 1.5, double.Parse(label.GetAttribute("x")!, System.Globalization.CultureInfo.InvariantCulture), 3));
        });
    }

    [Fact]
    public void ColumnSeries_WithoutColorByPoint_LeavesItsBarsWithoutClass()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Ventes")
            .AddChildContent(builder => Child<OmniColumnSeries>(builder, 0, ("Data", Points(3, 5)))));

        chart.WaitForAssertion(() => Assert.All(chart.FindAll(".omni-chart__columns rect"), bar => Assert.False(bar.HasAttribute("class"))));
    }

    [Fact]
    public void GaugeScaleWithoutRoom_DrawsItsValueAtTheMinimum()
    {
        var gauge = Render<OmniArcGauge>(parameters => parameters
            .Add(component => component.Label, "Charge")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniArcGaugeScale>(0);
                builder.AddComponentParameter(1, nameof(OmniArcGaugeScale.Minimum), 10d);
                builder.AddComponentParameter(2, nameof(OmniArcGaugeScale.Maximum), 10d);
                builder.AddComponentParameter(3, nameof(OmniArcGaugeScale.ChildContent), (RenderFragment)(inner => Child<OmniArcGaugeScaleValue>(inner, 0, ("Value", 50d))));
                builder.CloseComponent();
            }));

        Assert.Contains(">10<", gauge.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ValueAxis_WhoseMaximumDoesNotExceedItsMinimum_IsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniValueAxis>(parameters => parameters
            .Add(component => component.Minimum, 10d)
            .Add(component => component.Maximum, 10d)));

    [Fact]
    public void SharedTooltip_OverSeriesOfUnequalLength_NamesOnlyTheSeriesThatHaveThePoint_AndSkipsEmptyCategories()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Audience")
            .Add(component => component.SharedTooltip, true)
            .AddChildContent(builder =>
            {
                Child<OmniCategoryAxis>(builder, 0, ("Labels", new[] { "janv.", "févr.", "mars", "avril" }));
                Child<OmniLineSeries>(builder, 10, ("Data", Points(1, 2, 3)), ("Title", "Visites"));
                Child<OmniLineSeries>(builder, 20, ("Data", Points(4)), ("Title", "Achats"));
            }));

        chart.WaitForAssertion(() =>
        {
            var bands = chart.FindAll(".omni-chart__hover-band title").Select(title => title.TextContent).ToArray();
            // Avril has no point in any series: no band.
            Assert.Equal(3, bands.Length);
            Assert.Contains("Achats", bands[0], StringComparison.Ordinal);
            Assert.DoesNotContain("Achats", bands[1], StringComparison.Ordinal);
        });
    }

    [Fact]
    public void GridLines_OutsideAChart_KeepTheirOwnLayoutAcrossRenders()
    {
        var lines = Render<OmniGridLines>(parameters => parameters.Add(component => component.Count, 2));
        lines.Render(parameters => parameters.Add(component => component.Count, 3));

        Assert.Equal(3, lines.FindAll("line").Count);
        Assert.All(lines.FindAll("line"), line => Assert.Equal(line.GetAttribute("y1"), line.GetAttribute("y2")));
    }

    [Fact]
    public void Gauge_ForgettingAValueItNeverHad_ChangesNothing()
    {
        var gauge = Render<OmniArcGauge>(parameters => parameters.Add(component => component.Label, "Charge"));
        var renders = gauge.RenderCount;

        gauge.Instance.RemoveValue(new object());

        Assert.Equal(renders, gauge.RenderCount);
    }

    [Fact]
    public void Geometry_OfNoPoint_IsEmpty()
    {
        Assert.Equal(string.Empty, OmniChartGeometry.Points([]));
        Assert.Equal(string.Empty, OmniChartGeometry.AreaPoints([]));
    }
}
