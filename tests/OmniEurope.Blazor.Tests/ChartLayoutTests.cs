using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Where the parts of a chart land relative to each other, and the gauge's arc. Each test pins a
/// defect the OE Demo review found: gauges standing on their edge, a line series filled as a polygon,
/// column series covering each other, labels beside the wrong column, bars drawn on unturned axes.
/// </summary>
public sealed class ChartLayoutTests : OmniBunitContext
{
    /// <summary>The plot's left edge in a square chart (the default aspect ratio).</summary>
    private const double SquarePlotLeft = 14;

    [Fact]
    public void AspectRatio_WidensThePlot_KeepsTheLeftMarginAndCentresTheSquare()
    {
        // Aetheus recette R-177: a chart in a wide, low card drew a square and left the card empty.
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Tendance")
            .Add(component => component.AspectRatio, 3)
            .AddChildContent<OmniLineSeries>(series => series
                .Add(line => line.Title, "Réussis")
                .Add(line => line.Data, [new OmniChartPoint(0, 1), new OmniChartPoint(1, 3)])));

        // 300 wide, centred on the former square: x runs from -100 to 200.
        Assert.Equal("-100 0 300 100", chart.Find("svg.omni-chart__svg").GetAttribute("viewBox"));
        var xs = chart.Find(".omni-chart__line").GetAttribute("points")!
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(point => double.Parse(point.Split(',')[0], CultureInfo.InvariantCulture)).ToList();
        // Left margin of 14 from the view box edge, right margin of 4: the plot spans -86 to 196.
        Assert.Equal(-86, xs.Min(), 3);
        Assert.Equal(196, xs.Max(), 3);
    }

    [Fact]
    public void AspectRatio_BelowOne_KeepsTheSquare()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Tendance")
            .Add(component => component.AspectRatio, 0.5));

        Assert.Equal("0 0 100 100", chart.Find("svg.omni-chart__svg").GetAttribute("viewBox"));
    }

    [Fact]
    public void ArcGauge_DrawsAHalfCircleFromItsLeftEndOverTheTop()
    {
        // The arc used to start at the bottom of the circle and climb its left side, so the gauge
        // stood on its edge and half of it fell below the drawing.
        Assert.Equal("M 10 50 A 40 40 0 0 1 10 50", OmniChartGeometry.Gauge(0));
        Assert.Equal("M 10 50 A 40 40 0 0 1 50 10", OmniChartGeometry.Gauge(50));
        Assert.Equal("M 10 50 A 40 40 0 0 1 90 50", OmniChartGeometry.Gauge(100));
    }

    [Fact]
    public void ArcGaugeValue_TakesTheBoundsOfItsScale_UnlessItSetsItsOwn()
    {
        var inherited = Render<OmniArcGauge>(parameters => parameters
            .Add(component => component.Label, "Température")
            .AddChildContent<OmniArcGaugeScale>(scale => scale
                .Add(component => component.Minimum, -20)
                .Add(component => component.Maximum, 40)
                .AddChildContent<OmniArcGaugeScaleValue>(value => value.Add(component => component.Value, 10))));
        var own = Render<OmniArcGauge>(parameters => parameters
            .AddChildContent<OmniArcGaugeScale>(scale => scale
                .Add(component => component.Minimum, -20)
                .Add(component => component.Maximum, 40)
                .AddChildContent<OmniArcGaugeScaleValue>(value => value
                    .Add(component => component.Value, 25)
                    .Add(component => component.Minimum, 0)
                    .Add(component => component.Maximum, 100))));

        // 10 on a scale from -20 to 40 is half way, the top of the arc, and not 10 % of 0 to 100.
        Assert.Equal(OmniChartGeometry.Gauge(50), inherited.Find(".omni-arc-gauge__value path").GetAttribute("d"));
        Assert.Equal("10", inherited.Find(".omni-arc-gauge__value-text").TextContent);
        Assert.Equal(["-20", "40"], inherited.FindAll(".omni-arc-gauge__limit").Select(text => text.TextContent));
        Assert.Equal(OmniChartGeometry.Gauge(25), own.Find(".omni-arc-gauge__value path").GetAttribute("d"));
    }

    [Fact]
    public void ArcGaugeValue_FollowsTheScaleWhenItsBoundsChange()
    {
        var gauge = Render<OmniArcGaugeScale>(parameters => parameters
            .Add(component => component.Maximum, 100)
            .AddChildContent<OmniArcGaugeScaleValue>(value => value.Add(component => component.Value, 50)));
        Assert.Equal(OmniChartGeometry.Gauge(50), gauge.Find(".omni-arc-gauge__value path").GetAttribute("d"));

        gauge.Render(parameters => parameters.Add(component => component.Maximum, 200));

        Assert.Equal(OmniChartGeometry.Gauge(25), gauge.Find(".omni-arc-gauge__value path").GetAttribute("d"));
    }

    [Fact]
    public void ColumnSeries_StandSideBySideInTheBandOfTheirCategory()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Colonnes")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniCategoryAxis>(0);
                builder.AddAttribute(1, nameof(OmniCategoryAxis.Labels), Labels("T1", "T2"));
                builder.CloseComponent();
                builder.OpenComponent<OmniValueAxis>(2);
                builder.AddAttribute(3, nameof(OmniValueAxis.Maximum), 100d);
                builder.CloseComponent();
                builder.OpenComponent<OmniColumnSeries>(4);
                builder.AddAttribute(5, nameof(OmniColumnSeries.Data), Points((1, 40), (2, 60)));
                builder.CloseComponent();
                builder.OpenComponent<OmniColumnSeries>(6);
                builder.AddAttribute(7, nameof(OmniColumnSeries.Data), Points((1, 50), (2, 70)));
                builder.AddAttribute(8, nameof(OmniColumnSeries.ColorIndex), 1);
                builder.CloseComponent();
            }));

        chart.WaitForAssertion(() =>
        {
            var groups = chart.FindAll(".omni-chart__columns");
            var labels = chart.FindAll(".omni-chart__axis--category text");
            for (var index = 0; index < 2; index++)
            {
                var first = groups[0].QuerySelectorAll("rect")[index];
                var second = groups[1].QuerySelectorAll("rect")[index];
                var label = Number(labels[index], "x");

                // Beside each other, never over each other, with the label under the pair.
                Assert.True(Number(first, "x") + Number(first, "width") <= Number(second, "x"));
                Assert.InRange(label, Number(first, "x") + Number(first, "width"), Number(second, "x"));
            }
        });
    }

    [Fact]
    public void BarSeries_TurnTheChart_CategoriesDownTheLeftAndValuesAlongTheBottom()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Barres")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniCategoryAxis>(0);
                builder.AddAttribute(1, nameof(OmniCategoryAxis.Labels), Labels("T1", "T2"));
                builder.CloseComponent();
                builder.OpenComponent<OmniValueAxis>(2);
                builder.AddAttribute(3, nameof(OmniValueAxis.Maximum), 100d);
                builder.CloseComponent();
                builder.OpenComponent<OmniBarSeries>(4);
                builder.AddAttribute(5, nameof(OmniBarSeries.Data), Points((1, 40), (2, 60)));
                builder.CloseComponent();
            }));

        chart.WaitForAssertion(() =>
        {
            var bars = chart.FindAll(".omni-chart__bars rect");
            var categories = chart.FindAll(".omni-chart__axis--category text");
            var values = chart.FindAll(".omni-chart__axis--value text");
            Assert.All(categories, label => Assert.True(Number(label, "x") < SquarePlotLeft));
            Assert.All(values, label => Assert.True(Number(label, "y") > OmniChartContext.PlotBottom));
            for (var index = 0; index < 2; index++)
            {
                var label = Number(categories[index], "y");
                Assert.InRange(label, Number(bars[index], "y"), Number(bars[index], "y") + Number(bars[index], "height"));
            }

            Assert.Equal(0.4 * (96 - SquarePlotLeft), Number(bars[0], "width"), 3);
        });
    }

    [Fact]
    public void Legend_NarrowsThePlotAndTakesTheColoursOfTheSeriesItNames()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Légende")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniGridLines>(0);
                builder.CloseComponent();
                builder.OpenComponent<OmniLineSeries>(1);
                builder.AddAttribute(2, nameof(OmniLineSeries.Data), Points((0, 0), (1, 2)));
                builder.AddAttribute(3, nameof(OmniLineSeries.ColorIndex), 3);
                builder.CloseComponent();
                builder.OpenComponent<OmniLegend>(4);
                builder.AddAttribute(5, nameof(OmniLegend.Items), Labels("Série", "Autre"));
                builder.AddAttribute(6, nameof(OmniLegend.ColorIndexes), (IReadOnlyList<int>)[3]);
                builder.CloseComponent();
            }));

        chart.WaitForAssertion(() =>
        {
            // The grid lines and the line rendered before the legend registered, and still moved.
            Assert.All(chart.FindAll(".omni-chart__grid-lines line"), line => Assert.Equal("76", line.GetAttribute("x2")));
            Assert.Equal("14,86 76,4", chart.Find(".omni-chart__line").GetAttribute("points"));
            var swatches = chart.FindAll(".omni-chart__legend rect");
            Assert.Contains("omni-chart-color-3", swatches[0].ClassList);
            Assert.Contains("omni-chart-color-1", swatches[1].ClassList);
        });
    }

    [Fact]
    public void AspectRatio_Unset_WidensATimeSeries_ButKeepsAnExplicitSquareAndShortSeries()
    {
        // Aetheus recette R-353: thirty days drawn in a tall square left a card mostly empty.
        var wide = RenderDaily(aspectRatio: null, legend: null);
        var square = RenderDaily(aspectRatio: 1, legend: null);
        var shortSeries = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Trimestre")
            .AddChildContent<OmniLineSeries>(series => series.Add(line => line.Data, Points((0, 1), (1, 2), (2, 3)))));

        wide.WaitForAssertion(() => Assert.Equal("-50 0 200 100", wide.Find("svg.omni-chart__svg").GetAttribute("viewBox")));
        square.WaitForAssertion(() => Assert.Equal("0 0 100 100", square.Find("svg.omni-chart__svg").GetAttribute("viewBox")));
        Assert.Equal("0 0 100 100", shortSeries.Find("svg.omni-chart__svg").GetAttribute("viewBox"));
        // Only the wide drawing takes the class that enlarges its axis text on a narrow screen.
        Assert.Contains("omni-chart__svg--wide", wide.Find("svg.omni-chart__svg").ClassList);
        Assert.DoesNotContain("omni-chart__svg--wide", square.Find("svg.omni-chart__svg").ClassList);
    }

    [Fact]
    public void WideChart_ThinsItsLabelsForTheLargerAxisTextOfNarrowScreens()
    {
        var chart = RenderDaily(aspectRatio: null, legend: null);

        chart.WaitForAssertion(() =>
        {
            var xs = chart.FindAll(".omni-chart__axis--category text").Select(label => Number(label, "x")).ToList();
            Assert.Equal(10, xs.Count);
            Assert.All(xs.Zip(xs.Skip(1)), pair => Assert.True(
                pair.Second - pair.First >= (5 * OmniChartContext.CharacterWidth * OmniChartContext.WideAxisFontSize / OmniChartContext.FontSize) + 1.5));
        });

        var css = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "omnieurope.blazor.css"));
        Assert.Contains(
            FormattableString.Invariant($".omni-chart__svg--wide .omni-chart__axis text {{ font-size: {OmniChartContext.WideAxisFontSize}px; }}"),
            css,
            StringComparison.Ordinal);
    }

    [Fact]
    public void CategoryAxis_ThinsLabelsThatWouldOverlap_KeepingTheFirstAndTheLast()
    {
        // Aetheus recette R-354: thirty dates under a square plot ran into one unreadable block.
        var chart = RenderDaily(aspectRatio: 1, legend: null);

        chart.WaitForAssertion(() =>
        {
            var labels = chart.FindAll(".omni-chart__axis--category text");
            Assert.InRange(labels.Count, 2, 29);
            Assert.Equal("01/09", labels[0].TextContent);
            Assert.Equal("30/09", labels[^1].TextContent);
            // Five characters of 1.7 units each, plus a gap: no two drawn labels can touch.
            var xs = labels.Select(label => Number(label, "x")).ToList();
            Assert.All(xs.Zip(xs.Skip(1)), pair => Assert.True(pair.Second - pair.First >= (5 * OmniChartContext.CharacterWidth) + 1.5));
        });
    }

    [Fact]
    public void CategoryAxis_DrawsEveryLabelThatFits()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Semestre")
            .AddChildContent<OmniCategoryAxis>(axis => axis.Add(component => component.Labels, Labels("Jan", "Fév", "Mar", "Avr", "Mai", "Jui"))));

        Assert.Equal(["Jan", "Fév", "Mar", "Avr", "Mai", "Jui"], chart.FindAll(".omni-chart__axis--category text").Select(text => text.TextContent));
    }

    [Fact]
    public void ColumnHoverText_NamesItsCategory_SoAThinnedOutDateStaysReachable()
    {
        var chart = RenderDaily(aspectRatio: 1, legend: null);

        chart.WaitForAssertion(() =>
        {
            var titles = chart.FindAll(".omni-chart__columns rect title").Select(title => title.TextContent).ToList();
            Assert.Equal(30, titles.Count);
            Assert.Equal($"02/09 · {101.ToString(CultureInfo.CurrentCulture)}", titles[1]);
        });
    }

    [Fact]
    public void Legend_Auto_GoesBelowTheChartWhenItsEntriesAreLong_AndThePlotTakesTheFullWidth()
    {
        var chart = RenderDaily(aspectRatio: 1, legend: OmniLegendPosition.Auto);

        chart.WaitForAssertion(() =>
        {
            Assert.Empty(chart.FindAll("svg .omni-chart__legend"));
            var list = chart.Find("figure > ul.omni-chart__legend--below");
            Assert.False(string.IsNullOrWhiteSpace(list.GetAttribute("aria-label")));
            var items = list.QuerySelectorAll("li");
            Assert.Equal(["Visiteurs uniques par jour", "Pages vues par jour"], items.Select(item => item.TextContent));
            Assert.Contains("omni-chart-color-0", items[0].QuerySelector(".omni-chart__swatch")!.ClassList);
            Assert.Contains("omni-chart-color-1", items[1].QuerySelector(".omni-chart__swatch")!.ClassList);
            Assert.Equal("true", items[0].QuerySelector(".omni-chart__swatch")!.GetAttribute("aria-hidden"));
            Assert.All(chart.FindAll(".omni-chart__grid-lines line"), line => Assert.Equal("96", line.GetAttribute("x2")));
        });
    }

    [Fact]
    public void Legend_Right_WidensItsColumnToTheLongestEntry_UpToTwoFifthsOfTheDrawing()
    {
        var chart = RenderDaily(aspectRatio: 1, legend: OmniLegendPosition.Right);

        chart.WaitForAssertion(() =>
        {
            Assert.Empty(chart.FindAll("ul.omni-chart__legend--below"));
            Assert.Equal(2, chart.FindAll("svg .omni-chart__legend text").Count);
            // "Visiteurs uniques par jour" needs more than the 24 default; the column stops at 40.
            Assert.All(chart.FindAll(".omni-chart__grid-lines line"), line => Assert.Equal("60", line.GetAttribute("x2")));
        });
    }

    [Fact]
    public void Legend_Bottom_LeavesNothingInsideTheDrawing()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Légende")
            .AddChildContent<OmniLegend>(legend => legend
                .Add(component => component.Items, Labels("A"))
                .Add(component => component.Position, OmniLegendPosition.Bottom)));

        chart.WaitForAssertion(() =>
        {
            Assert.Empty(chart.FindAll("svg .omni-chart__legend"));
            Assert.Equal("A", chart.Find("ul.omni-chart__legend--below li").TextContent);
        });
    }

    [Fact]
    public void SingleSlice_IsAWholeDiscWithoutANotch()
    {
        var pie = Render<OmniPieSeries>(parameters => parameters.Add(component => component.Data, [new OmniChartSlice("Tout", 10)]));
        var donut = Render<OmniDonutSeries>(parameters => parameters.Add(component => component.Data, [new OmniChartSlice("Tout", 10)]));

        Assert.Equal("M 50 8 A 42 42 0 1 1 50 92 A 42 42 0 1 1 50 8 Z", pie.Find("path").GetAttribute("d"));
        Assert.EndsWith("A 24.36 24.36 0 1 0 50 25.64 Z", donut.Find("path").GetAttribute("d"), StringComparison.Ordinal);
    }

    [Fact]
    public void Stylesheet_KeepsLinesAndMarkersUnfilledDespiteTheColourClasses()
    {
        // A colour class sets fill too. Declared after the line rule with the same weight, it filled
        // every line series as a polygon; the overrides must outrank it. The rendered result was
        // checked in Chromium; this pins the rules that produce it.
        var css = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "omnieurope.blazor.css"));
        var lastColour = css.LastIndexOf(".omni-chart-color-7 {", StringComparison.Ordinal);

        Assert.True(lastColour > 0);
        Assert.True(css.IndexOf("polyline.omni-chart__line { fill: none; }", StringComparison.Ordinal) > lastColour);
        Assert.True(css.IndexOf(".omni-chart__markers circle { fill: var(--omni-color-surface); }", StringComparison.Ordinal) > lastColour);
    }

    /// <summary>
    /// The Aetheus daily audience: thirty dates, a column and a line series, and a legend with long
    /// entries when <paramref name="legend"/> is given.
    /// </summary>
    private IRenderedComponent<OmniChart> RenderDaily(double? aspectRatio, OmniLegendPosition? legend)
    {
        var days = Enumerable.Range(1, 30).Select(day => $"{day:00}/09").ToArray();
        var views = Enumerable.Range(0, 30).Select(day => new OmniChartPoint(day, 100 + day)).ToArray();
        var visitors = Enumerable.Range(0, 30).Select(day => new OmniChartPoint(day, 40 + day)).ToArray();
        return Render<OmniChart>(parameters =>
        {
            parameters.Add(component => component.Title, "Audience");
            if (aspectRatio is { } ratio)
            {
                parameters.Add(component => component.AspectRatio, ratio);
            }

            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<OmniGridLines>(0);
                builder.CloseComponent();
                builder.OpenComponent<OmniCategoryAxis>(1);
                builder.AddAttribute(2, nameof(OmniCategoryAxis.Labels), (IReadOnlyList<string>)days);
                builder.CloseComponent();
                builder.OpenComponent<OmniColumnSeries>(3);
                builder.AddAttribute(4, nameof(OmniColumnSeries.Data), (IReadOnlyList<OmniChartPoint>)views);
                builder.AddAttribute(5, nameof(OmniColumnSeries.ColorIndex), 1);
                builder.CloseComponent();
                builder.OpenComponent<OmniLineSeries>(6);
                builder.AddAttribute(7, nameof(OmniLineSeries.Data), (IReadOnlyList<OmniChartPoint>)visitors);
                builder.CloseComponent();
                if (legend is { } position)
                {
                    builder.OpenComponent<OmniLegend>(8);
                    builder.AddAttribute(9, nameof(OmniLegend.Items), Labels("Visiteurs uniques par jour", "Pages vues par jour"));
                    builder.AddAttribute(10, nameof(OmniLegend.Position), position);
                    builder.CloseComponent();
                }
            });
        });
    }

    private static IReadOnlyList<string> Labels(params string[] labels) => labels;

    private static IReadOnlyList<OmniChartPoint> Points(params (double X, double Y)[] points) =>
        [.. points.Select(point => new OmniChartPoint(point.X, point.Y))];

    private static double Number(AngleSharp.Dom.IElement element, string attribute) =>
        double.Parse(element.GetAttribute(attribute)!, CultureInfo.InvariantCulture);

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OmniEurope.Blazor.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }
}
