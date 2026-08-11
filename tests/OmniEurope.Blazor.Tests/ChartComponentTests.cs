using Bunit;
using OmniEurope.Blazor.Components;
using System.Globalization;

namespace OmniEurope.Blazor.Tests;

public sealed class ChartComponentTests : BunitContext
{
    [Fact]
    public void ChartContext_ComputesDomainsOncePerRegistrationSnapshot()
    {
        var context = new OmniChartContext();
        var owner = new object();
        context.RegisterSeries(owner, OmniChartSeriesKind.Line,
            Enumerable.Range(0, 1_000).Select(index => new OmniChartPoint(index, index % 17)).ToArray());

        _ = context.Points(owner);
        _ = context.Points(owner);

        Assert.Equal(1, context.DomainCalculationCount);

        context.RegisterValueAxis(new object(), 0, 100);
        _ = context.Points(owner);

        Assert.Equal(2, context.DomainCalculationCount);
    }

    [Fact]
    public void Charts_RenderSemanticSvgSeriesAndAccessibleAlternative()
    {
        var charts = Render<ChartTestHost>();

        Assert.Equal(2, charts.FindAll(".omni-chart__svg").Count);
        Assert.Equal("img", charts.Find("#sales .omni-chart__svg").GetAttribute("role"));
        Assert.Contains("Ventes mensuelles", charts.Find("#sales desc").TextContent, StringComparison.Ordinal);
        Assert.Equal(3, charts.FindAll("#sales .omni-chart__markers circle").Count);
        Assert.Equal(4, charts.FindAll(".omni-chart__pie path, .omni-chart__donut path").Count);
        Assert.Contains("Données des ventes", charts.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", charts.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicBarColumnAreaAndOptions_RenderNegativeEmptyAndStackedStates()
    {
        var points = new[] { new OmniChartPoint(0, -5, "Loss"), new OmniChartPoint(1, 10, "Gain") };
        var bars = Render<OmniBarSeries>(parameters => parameters
            .Add(component => component.Data, points)
            .Add(component => component.Title, "Bars"));
        var columns = Render<OmniColumnSeries>(parameters => parameters
            .Add(component => component.Data, points)
            .Add(component => component.Title, "Columns"));
        var area = Render<OmniStackedAreaSeries>(parameters => parameters
            .Add(component => component.Data, points)
            .Add(component => component.Title, "Area"));
        var empty = Render<OmniBarSeries>();
        var options = Render<OmniBarOptions>(parameters => parameters
            .Add(component => component.Orientation, "vertical")
            .Add(component => component.Stacked, true)
            .AddChildContent("Series"));

        Assert.Equal(2, bars.FindAll("rect").Count);
        Assert.All(bars.FindAll("rect"), rectangle => Assert.True(Number(rectangle, "width") >= 0));
        Assert.Equal(2, columns.FindAll("rect").Count);
        Assert.All(columns.FindAll("rect"), rectangle => Assert.True(Number(rectangle, "height") >= 0));
        Assert.Equal("Bars", bars.Find("g").GetAttribute("aria-label"));
        Assert.Equal("Columns", columns.Find("g").GetAttribute("aria-label"));
        Assert.Contains("Loss", bars.Markup, StringComparison.Ordinal);
        Assert.Contains("Loss", columns.Markup, StringComparison.Ordinal);
        Assert.StartsWith("5,95", area.Find("polygon").GetAttribute("points"), StringComparison.Ordinal);
        Assert.Contains("Area", area.Markup, StringComparison.Ordinal);
        Assert.Empty(empty.FindAll("rect"));
        Assert.Equal("vertical", options.Find("g").GetAttribute("data-orientation"));
        Assert.Contains("omni-chart__bar-options--stacked", options.Find("g").ClassList);
        Assert.Contains("Series", options.Markup, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GridLines_RejectsNonPositiveCounts(int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniGridLines>(parameters => parameters.Add(component => component.Count, count)));
    }

    [Fact]
    public void ArcGauge_ClampsAndAnnouncesItsValue()
    {
        var gauge = Render<ChartTestHost>();
        Assert.Contains("75", gauge.Find(".omni-arc-gauge__value text").TextContent, StringComparison.Ordinal);
        Assert.Equal("Progression", gauge.Find(".omni-arc-gauge__svg").GetAttribute("aria-label"));
        Assert.StartsWith("M ", gauge.Find(".omni-arc-gauge__value path").GetAttribute("d"), StringComparison.Ordinal);

        var below = Render<OmniArcGaugeScaleValue>(parameters => parameters
            .Add(component => component.Minimum, 0)
            .Add(component => component.Maximum, 100)
            .Add(component => component.Value, -25));
        var above = Render<OmniArcGaugeScaleValue>(parameters => parameters
            .Add(component => component.Minimum, 0)
            .Add(component => component.Maximum, 100)
            .Add(component => component.Value, 125));
        var minimum = Render<OmniArcGaugeScaleValue>(parameters => parameters.Add(component => component.Value, 0));
        var maximum = Render<OmniArcGaugeScaleValue>(parameters => parameters.Add(component => component.Value, 100));

        Assert.Equal("0", below.Find("text").TextContent);
        Assert.Equal("100", above.Find("text").TextContent);
        Assert.Equal(minimum.Find("path").GetAttribute("d"), below.Find("path").GetAttribute("d"));
        Assert.Equal(maximum.Find("path").GetAttribute("d"), above.Find("path").GetAttribute("d"));
    }

    [Fact]
    public void CartesianSeries_UseTheSharedExplicitAxisDomain()
    {
        var charts = Render<ChartProjectionTestHost>();

        charts.WaitForAssertion(() =>
            Assert.Equal("5,95 95,5", charts.Find("#projection-domain .omni-chart__line").GetAttribute("points")));
    }

    [Fact]
    public void StackedColumns_AccumulatePositiveAndNegativeValuesOnTheSharedDomain()
    {
        var charts = Render<ChartProjectionTestHost>();

        charts.WaitForAssertion(() =>
        {
            var groups = charts.FindAll("#stacked-domain .omni-chart__columns--stacked");
            Assert.Equal(2, groups.Count);
            var first = groups[0].QuerySelectorAll("rect");
            var second = groups[1].QuerySelectorAll("rect");

            Assert.Equal(Number(first[0], "y"), Number(second[0], "y") + Number(second[0], "height"), 8);
            Assert.Equal(Number(first[1], "y") + Number(first[1], "height"), Number(second[1], "y"), 8);
        });
    }

    [Fact]
    public void Markers_FormatSvgLengthsWithInvariantCulture()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var markers = Render<OmniMarkers>(parameters => parameters
                .Add(component => component.Data, [new OmniChartPoint(10, 20)]));

            Assert.Equal("1.5", markers.Find("circle").GetAttribute("r"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    private static double Number(AngleSharp.Dom.IElement element, string attribute) =>
        double.Parse(element.GetAttribute(attribute)!, CultureInfo.InvariantCulture);
}
