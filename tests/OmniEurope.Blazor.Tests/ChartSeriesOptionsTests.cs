using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The chart options the migration asked for: dashed series, one shared hover text per category,
/// and a legend above the drawing.
/// </summary>
public sealed class ChartSeriesOptionsTests : OmniBunitContext
{
    [Fact]
    public void Dashed_AddsTheDashedModifierToALineAndToAnAreaOutline_OnlyWhenAsked()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Objectif")
            .AddChildContent(builder =>
            {
                Line(builder, 0, "Plein", Points(1, 2, 3), dashed: false);
                Line(builder, 10, "Objectif", Points(2, 2, 2), dashed: true);
                Area(builder, 20, "Aire", Points(1, 1, 1), dashed: true);
                Area(builder, 30, "Aire pleine", Points(1, 2, 1), dashed: false);
            }));

        chart.WaitForAssertion(() =>
        {
            var lines = chart.FindAll("polyline.omni-chart__line");
            Assert.DoesNotContain("omni-chart__line--dashed", lines[0].ClassList);
            Assert.Contains("omni-chart__line--dashed", lines[1].ClassList);
            var areas = chart.FindAll("polygon.omni-chart__area");
            // The area keeps its base class, hence its fill; only the outline takes the dashes.
            Assert.Contains("omni-chart__area--dashed", areas[0].ClassList);
            Assert.Contains("omni-chart__area", areas[0].ClassList);
            Assert.DoesNotContain("omni-chart__area--dashed", areas[1].ClassList);
            Assert.DoesNotContain("style=", chart.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Dashed_StackedArea_KeepsBothModifiers()
    {
        var area = Render<OmniAreaSeries>(parameters => parameters
            .Add(component => component.Data, Points(1, 2))
            .Add(component => component.Stacked, true)
            .Add(component => component.Dashed, true));

        var classes = area.Find("polygon").ClassList;
        Assert.Contains("omni-chart__area--stacked", classes);
        Assert.Contains("omni-chart__area--dashed", classes);
    }

    [Fact]
    public void Stylesheet_DashesTheModifiers_AndKeepsTheirStrokeNonScaling()
    {
        var css = StylesheetSource.Read();

        Assert.Matches(@"\.omni-chart__line--dashed \{ stroke-dasharray: [0-9 ]+; \}", css);
        Assert.Matches(@"\.omni-chart__area--dashed \{ stroke-dasharray: [0-9 ]+; \}", css);
        // The dashed elements keep their base classes, which hold the non-scaling stroke.
        Assert.Matches(@"\.omni-chart__line,\s*\.omni-chart__area,[^{]*\{ vector-effect: non-scaling-stroke; \}", css);
        Assert.Contains(".omni-chart__hover-band { fill: transparent; pointer-events: all; }", css, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedTooltip_Off_DrawsNoBand_AndKeepsThePerPointHoverTexts()
    {
        var chart = RenderMonthly(shared: false, format: null);

        chart.WaitForAssertion(() =>
        {
            Assert.Empty(chart.FindAll(".omni-chart__hover"));
            Assert.Equal(3, chart.FindAll(".omni-chart__columns rect title").Count);
            Assert.Equal($"Fév · {12.ToString(CultureInfo.CurrentCulture)}", chart.FindAll(".omni-chart__columns rect title")[1].TextContent);
        });
    }

    [Fact]
    public void SharedTooltip_DrawsOneHiddenBandPerCategory_OverTheSeries_NamingEverySeriesInOrder()
    {
        var chart = RenderMonthly(shared: true, format: null);

        chart.WaitForAssertion(() =>
        {
            var group = chart.Find(".omni-chart__hover");
            Assert.Equal("true", group.GetAttribute("aria-hidden"));
            // Last in the drawing, so the bands lie over the series and catch the pointer.
            Assert.Contains("omni-chart__hover", chart.Find("svg").Children.Last().ClassList);
            var bands = chart.FindAll(".omni-chart__hover-band");
            Assert.Equal(3, bands.Count);
            var culture = CultureInfo.CurrentCulture;
            Assert.Equal(
                $"Fév\nVentes · {12.ToString(culture)}\nSérie 2 · {7.5.ToString(culture)}",
                bands[1].QuerySelector("title")!.TextContent);
            Assert.Empty(chart.FindAll(".omni-chart__hover-band[style]"));
        });
    }

    [Fact]
    public void SharedTooltip_BandsTileThePlot_OneCategoryBandEach()
    {
        var chart = RenderMonthly(shared: true, format: null);

        chart.WaitForAssertion(() =>
        {
            var bands = chart.FindAll(".omni-chart__hover-band");
            var plotLeft = 14d;
            var plotRight = 96d;
            var band = (plotRight - plotLeft) / 3;
            for (var index = 0; index < bands.Count; index++)
            {
                Assert.Equal(plotLeft + (index * band), Number(bands[index], "x"), 3);
                Assert.Equal(band, Number(bands[index], "width"), 3);
                Assert.Equal(OmniChartContext.PlotTop, Number(bands[index], "y"), 3);
                Assert.Equal(OmniChartContext.PlotBottom - OmniChartContext.PlotTop, Number(bands[index], "height"), 3);
            }

            // The column of a category stands inside that category's band.
            var column = chart.FindAll(".omni-chart__columns rect")[2];
            var middle = Number(column, "x") + (Number(column, "width") / 2);
            Assert.InRange(middle, Number(bands[2], "x"), Number(bands[2], "x") + Number(bands[2], "width"));
        });
    }

    [Fact]
    public void SharedTooltip_LinesAlone_SplitThePlotHalfwayBetweenTheirPoints()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Courbe")
            .Add(component => component.SharedTooltip, true)
            .AddChildContent(builder => Line(builder, 0, "Taux", Points(1, 2, 3), dashed: false)));

        chart.WaitForAssertion(() =>
        {
            var bands = chart.FindAll(".omni-chart__hover-band");
            Assert.Equal(3, bands.Count);
            // Points at 14, 55 and 96: the bands meet halfway, at 34.5 and 75.5.
            Assert.Equal(14, Number(bands[0], "x"), 3);
            Assert.Equal(20.5, Number(bands[0], "width"), 3);
            Assert.Equal(34.5, Number(bands[1], "x"), 3);
            Assert.Equal(41, Number(bands[1], "width"), 3);
            Assert.Equal(96, Number(bands[2], "x") + Number(bands[2], "width"), 3);
            // Without a category axis, the category is the X value of the point.
            Assert.StartsWith($"{1.ToString(CultureInfo.CurrentCulture)}\nTaux · ", bands[1].QuerySelector("title")!.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void SharedTooltip_WritesTheValuesWithTheValueAxisFormat()
    {
        var chart = RenderMonthly(shared: true, format: value => $"{value:0.00} %");

        chart.WaitForAssertion(() =>
        {
            var text = chart.FindAll(".omni-chart__hover-band title")[0].TextContent;
            var culture = CultureInfo.CurrentCulture;
            Assert.Equal($"Jan\nVentes · {10.ToString("0.00", culture)} %\nSérie 2 · {5.ToString("0.00", culture)} %", text);
        });
    }

    [Fact]
    public void SharedTooltip_HorizontalBars_StackTheBandsDownThePlot()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Pays")
            .Add(component => component.SharedTooltip, true)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniCategoryAxis>(0);
                builder.AddAttribute(1, nameof(OmniCategoryAxis.Labels), (IReadOnlyList<string>)["FR", "DE"]);
                builder.CloseComponent();
                builder.OpenComponent<OmniColumnSeries>(2);
                builder.AddAttribute(3, nameof(OmniColumnSeries.Data), Points(4, 6));
                builder.AddAttribute(4, nameof(OmniColumnSeries.Horizontal), true);
                builder.AddAttribute(5, nameof(OmniColumnSeries.Title), "Dossiers");
                builder.CloseComponent();
            }));

        chart.WaitForAssertion(() =>
        {
            var bands = chart.FindAll(".omni-chart__hover-band");
            Assert.Equal(2, bands.Count);
            var half = (OmniChartContext.PlotBottom - OmniChartContext.PlotTop) / 2;
            Assert.Equal(OmniChartContext.PlotTop, Number(bands[0], "y"), 3);
            Assert.Equal(half, Number(bands[0], "height"), 3);
            Assert.Equal(14, Number(bands[1], "x"), 3);
            Assert.Equal($"DE\nDossiers · {6.ToString(CultureInfo.CurrentCulture)}", bands[1].QuerySelector("title")!.TextContent);
        });
    }

    [Fact]
    public void SharedTooltip_PieAlone_DrawsNoBand()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Origine")
            .Add(component => component.SharedTooltip, true)
            .AddChildContent<OmniPieSeries>(pie => pie.Add(component => component.Data, [new OmniChartSlice("A", 1), new OmniChartSlice("B", 2)])));

        chart.WaitForAssertion(() =>
        {
            Assert.Equal(2, chart.FindAll(".omni-chart__pie path").Count);
            Assert.Empty(chart.FindAll(".omni-chart__hover"));
        });
    }

    [Fact]
    public void Legend_Top_IsAnHtmlListBeforeTheDrawing_AndThePlotTakesTheFullWidth()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Légende")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniGridLines>(0);
                builder.CloseComponent();
                Line(builder, 1, "Taux", Points(1, 2), dashed: false);
                builder.OpenComponent<OmniLegend>(20);
                builder.AddAttribute(21, nameof(OmniLegend.Position), OmniLegendPosition.Top);
                builder.CloseComponent();
            }));

        chart.WaitForAssertion(() =>
        {
            Assert.Empty(chart.FindAll("svg .omni-chart__legend"));
            Assert.Empty(chart.FindAll("ul.omni-chart__legend--below"));
            var figure = chart.Find("figure");
            var list = figure.Children[0];
            Assert.Equal("UL", list.TagName);
            Assert.Contains("omni-chart__legend--above", list.ClassList);
            Assert.False(string.IsNullOrWhiteSpace(list.GetAttribute("aria-label")));
            Assert.Equal("svg", figure.Children[1].LocalName);
            Assert.Equal("Taux", list.QuerySelector("li")!.TextContent);
            Assert.All(chart.FindAll(".omni-chart__grid-lines line"), line => Assert.Equal("96", line.GetAttribute("x2")));
        });
    }

    [Fact]
    public void Stylesheet_LaysTheLegendAboveOutLikeTheOneBelow()
    {
        var css = StylesheetSource.Read();

        Assert.Contains(".omni-chart__legend--below,\n.omni-chart__legend--above { color: var(--omni-color-text); display: flex;", css.ReplaceLineEndings("\n"), StringComparison.Ordinal);
        Assert.Contains(".omni-chart__legend--above { margin: 0 0 var(--omni-space-xs); }", css, StringComparison.Ordinal);
    }

    /// <summary>
    /// Three months, a titled column series and an untitled line series (named by the localized
    /// fallback), with the shared tooltip when <paramref name="shared"/> and an axis format when given.
    /// </summary>
    private IRenderedComponent<OmniChart> RenderMonthly(bool shared, Func<double, string>? format) =>
        Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Ventes")
            .Add(component => component.SharedTooltip, shared)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniCategoryAxis>(0);
                builder.AddAttribute(1, nameof(OmniCategoryAxis.Labels), (IReadOnlyList<string>)["Jan", "Fév", "Mar"]);
                builder.CloseComponent();
                builder.OpenComponent<OmniValueAxis>(2);
                builder.AddAttribute(3, nameof(OmniValueAxis.Maximum), 20d);
                if (format is not null)
                {
                    builder.AddAttribute(4, nameof(OmniValueAxis.FormatValue), format);
                }

                builder.CloseComponent();
                builder.OpenComponent<OmniColumnSeries>(5);
                builder.AddAttribute(6, nameof(OmniColumnSeries.Data), Points(10, 12, 14));
                builder.AddAttribute(7, nameof(OmniColumnSeries.Title), "Ventes");
                builder.CloseComponent();
                builder.OpenComponent<OmniLineSeries>(8);
                builder.AddAttribute(9, nameof(OmniLineSeries.Data), Points(5, 7.5, 9));
                builder.CloseComponent();
            }));

    private static void Line(RenderTreeBuilder builder, int sequence, string title, IReadOnlyList<OmniChartPoint> data, bool dashed)
    {
        builder.OpenComponent<OmniLineSeries>(sequence);
        builder.AddAttribute(sequence + 1, nameof(OmniLineSeries.Data), data);
        builder.AddAttribute(sequence + 2, nameof(OmniLineSeries.Title), title);
        builder.AddAttribute(sequence + 3, nameof(OmniLineSeries.Dashed), dashed);
        builder.CloseComponent();
    }

    private static void Area(RenderTreeBuilder builder, int sequence, string title, IReadOnlyList<OmniChartPoint> data, bool dashed)
    {
        builder.OpenComponent<OmniAreaSeries>(sequence);
        builder.AddAttribute(sequence + 1, nameof(OmniAreaSeries.Data), data);
        builder.AddAttribute(sequence + 2, nameof(OmniAreaSeries.Title), title);
        builder.AddAttribute(sequence + 3, nameof(OmniAreaSeries.Dashed), dashed);
        builder.CloseComponent();
    }

    private static IReadOnlyList<OmniChartPoint> Points(params double[] values) =>
        [.. values.Select((value, index) => new OmniChartPoint(index, value))];

    private static double Number(AngleSharp.Dom.IElement element, string attribute) =>
        double.Parse(element.GetAttribute(attribute)!, CultureInfo.InvariantCulture);
}
