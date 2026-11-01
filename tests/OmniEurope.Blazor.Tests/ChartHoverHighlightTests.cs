using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// What the pointer finds on a chart, on by default: the nearest point of a line, the column or bar under
/// it, and a label inside a bar that reads on that bar. The hover states themselves, the package tooltip
/// and the colour of a label are computed by a browser, not here.
/// </summary>
public sealed class ChartHoverHighlightTests : OmniBunitContext
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

    private IRenderedComponent<OmniChart> Chart(Action<RenderTreeBuilder> parts, bool highlight = true, bool shared = false) =>
        Render<OmniChart>(parameters => parameters
            .Add(component => component.Label, "Coûts")
            .Add(component => component.HighlightOnHover, highlight)
            .Add(component => component.SharedTooltip, shared)
            .AddChildContent(parts.Invoke));

    private static (double X, double Y)[] Reach(AngleSharp.Dom.IElement zone) =>
        [.. zone.QuerySelector(".omni-chart__point-reach")!.GetAttribute("points")!.Split(' ').Select(pair =>
        {
            var parts = pair.Split(',');
            return (double.Parse(parts[0], CultureInfo.InvariantCulture), double.Parse(parts[1], CultureInfo.InvariantCulture));
        })];

    private static (double X, double Y) Dot(AngleSharp.Dom.IElement zone)
    {
        var dot = zone.QuerySelector(".omni-chart__point-mark")!;
        return (double.Parse(dot.GetAttribute("cx")!, CultureInfo.InvariantCulture), double.Parse(dot.GetAttribute("cy")!, CultureInfo.InvariantCulture));
    }

    private IRenderedComponent<OmniChart> TwoLines(bool isolate) =>
        Render<OmniChart>(parameters => parameters
            .Add(component => component.Label, "Coûts")
            .Add(component => component.IsolateLineOnClick, isolate)
            .AddChildContent(builder =>
            {
                Child<OmniValueAxis>(builder, 0, ("Automatic", true));
                Child<OmniLineSeries>(builder, 10, ("Data", Points(10, 20)), ("Title", "Petite"), ("ColorIndex", 0));
                Child<OmniLineSeries>(builder, 20, ("Data", Points(1000, 2000)), ("Title", "Grande"), ("ColorIndex", 1));
                Child<OmniLegend>(builder, 30);
            }));

    private static string Ticks(IRenderedComponent<OmniChart> chart) =>
        string.Join('|', chart.FindAll(".omni-chart__axis text").Select(text => text.TextContent));

    [Fact]
    public void IsolateLineOnClick_KeepsTheSeriesClicked_RescalesTheAxis_AndTheLegendBringsTheOtherBack()
    {
        var chart = TwoLines(isolate: true);
        chart.WaitForAssertion(() => Assert.Equal(2, chart.FindAll("polyline.omni-chart__line").Count));
        var ticks = Ticks(chart);
        Assert.Contains("omni-chart--isolate-line", chart.Find("figure").ClassList);

        chart.FindAll(".omni-chart__point-zone.omni-chart-color-0")[0].Click();

        chart.WaitForAssertion(() =>
        {
            Assert.Contains("omni-chart-color-0", Assert.Single(chart.FindAll("polyline.omni-chart__line")).ClassList);
            Assert.NotEqual(ticks, Ticks(chart));
        });

        chart.Find("[aria-pressed=\"false\"]").Click();

        chart.WaitForAssertion(() =>
        {
            Assert.Equal(2, chart.FindAll("polyline.omni-chart__line").Count);
            Assert.Equal(ticks, Ticks(chart));
        });
    }

    [Fact]
    public void IsolateLineOnClick_IsOffByDefault_AndTheZonesTakeNoClick()
    {
        var chart = TwoLines(isolate: false);
        chart.WaitForAssertion(() => Assert.Equal(2, chart.FindAll("polyline.omni-chart__line").Count));

        Assert.DoesNotContain("omni-chart--isolate-line", chart.Find("figure").ClassList);
        Assert.Throws<MissingEventHandlerException>(() => chart.FindAll(".omni-chart__point-zone")[0].Click());
    }

    [Fact]
    public void ALine_IsCutIntoOneZonePerPoint_MeetingHalfwayBetweenPoints()
    {
        var chart = Chart(builder => Child<OmniLineSeries>(builder, 0, ("Data", Points(10, 40, 20))));

        chart.WaitForAssertion(() =>
        {
            var zones = chart.FindAll(".omni-chart__point-zone");
            Assert.Equal(3, zones.Count);
            var (first, middle, last) = (Reach(zones[0]), Reach(zones[1]), Reach(zones[2]));
            // The first and last points have one half, the middle one both; each zone holds its point.
            Assert.Equal(2, first.Length);
            Assert.Equal(3, middle.Length);
            Assert.Equal(2, last.Length);
            Assert.Equal(Dot(zones[0]), first[0]);
            Assert.Equal(Dot(zones[1]), middle[1]);
            Assert.Equal(Dot(zones[2]), last[^1]);
            // Each zone ends where the next begins, at the middle of the segment: no gap, no overlap.
            Assert.Equal(first[^1], middle[0]);
            Assert.Equal(middle[^1], last[0]);
            Assert.Equal((Dot(zones[0]).X + Dot(zones[1]).X) / 2, middle[0].X, 2);
            Assert.Equal((Dot(zones[0]).Y + Dot(zones[1]).Y) / 2, middle[0].Y, 2);
        });
    }

    [Fact]
    public void TheZonesComeAfterTheMarkers_SoThePointerFindsThemOnADot()
    {
        var data = Points(10, 40);
        var chart = Chart(builder =>
        {
            Child<OmniLineSeries>(builder, 0, ("Data", data));
            Child<OmniMarkers>(builder, 10, ("Data", data));
        });

        chart.WaitForAssertion(() =>
        {
            var groups = chart.Find(".omni-chart__parts").Children.Select(child => child.GetAttribute("class") ?? string.Empty).ToList();
            Assert.True(groups.FindIndex(name => name.Contains("omni-chart__markers", StringComparison.Ordinal))
                < groups.FindIndex(name => name.Contains("omni-chart__point-hover", StringComparison.Ordinal)));
            Assert.Equal("true", chart.Find(".omni-chart__point-hover").GetAttribute("aria-hidden"));
        });
    }

    [Fact]
    public void AZone_ShowsTheLabelOfTheHostAsItIs_InThePackageTooltip()
    {
        var chart = Chart(builder => Child<OmniLineSeries>(builder, 0, ("Title", "Modèle"), ("ColorIndex", 3),
            ("Data", new[] { new OmniChartPoint(0, 12, "Nord · 12 €"), new OmniChartPoint(1, 30, "Sud · 30 €") })));

        chart.WaitForAssertion(() =>
        {
            var zones = chart.FindAll(".omni-chart__point-zone");
            Assert.Equal(["Nord · 12 €", "Sud · 30 €"], zones.Select(zone => zone.GetAttribute("data-omni-tip")));
            Assert.Contains("omni-chart-color-3", zones[0].GetAttribute("class"), StringComparison.Ordinal);
        });
    }

    [Fact]
    public void WithoutALabel_AZone_NamesTheSeries_TheCategoryAndTheValue()
    {
        var chart = Chart(builder =>
        {
            Child<OmniCategoryAxis>(builder, 0, ("Labels", new[] { "lun.", "mar." }));
            Child<OmniLineSeries>(builder, 10, ("Title", "Opus"), ("Data", Points(12, 30)));
            Child<OmniLineSeries>(builder, 20, ("ColorIndex", 1), ("Data", Points(5, 6)));
        });

        chart.WaitForAssertion(() =>
        {
            var texts = chart.FindAll(".omni-chart__point-zone").Select(zone => zone.GetAttribute("data-omni-tip")).ToList();
            Assert.Equal(["Opus · lun. · 12", "Opus · mar. · 30", "lun. · 5", "mar. · 6"], texts);
        });
    }

    [Fact]
    public void ALonePoint_StillHasAZoneToHover()
    {
        var chart = Chart(builder => Child<OmniLineSeries>(builder, 0, ("Data", Points(10))));

        chart.WaitForAssertion(() =>
        {
            var reach = Reach(chart.Find(".omni-chart__point-zone"));
            Assert.Equal(2, reach.Length);
            Assert.Equal(reach[0], reach[1]);
        });
    }

    [Fact]
    public void TheZonesLeaveAColumnChartAlone()
    {
        var chart = Chart(builder => Child<OmniColumnSeries>(builder, 0, ("Data", Points(10, 20))));

        chart.WaitForAssertion(() => Assert.Equal(2, chart.FindAll(".omni-chart__columns rect").Count));
        Assert.Empty(chart.FindAll(".omni-chart__point-hover"));
    }

    [Fact]
    public void Off_TheChartDrawsNoZone_AndItsBarsStayStill()
    {
        var chart = Chart(builder => Child<OmniLineSeries>(builder, 0, ("Data", Points(10, 20))), highlight: false);

        chart.WaitForAssertion(() => Assert.Single(chart.FindAll("polyline.omni-chart__line")));
        Assert.Empty(chart.FindAll(".omni-chart__point-zone"));
        Assert.Contains("omni-chart--no-highlight", chart.Find("figure").GetAttribute("class"), StringComparison.Ordinal);
    }

    [Fact]
    public void On_TheFigureCarriesNoClassOfItsOwn()
    {
        var chart = Chart(builder => Child<OmniColumnSeries>(builder, 0, ("Data", Points(10, 20))));

        Assert.DoesNotContain("omni-chart--no-highlight", chart.Find("figure").GetAttribute("class"), StringComparison.Ordinal);
    }

    [Fact]
    public void ASharedTooltip_Replaces_TheHover_ItWouldCover()
    {
        var chart = Chart(builder => Child<OmniLineSeries>(builder, 0, ("Data", Points(10, 20))), shared: true);

        chart.WaitForAssertion(() => Assert.Equal(2, chart.FindAll(".omni-chart__hover-band").Count));
        Assert.Empty(chart.FindAll(".omni-chart__point-zone"));
        Assert.Contains("omni-chart--no-highlight", chart.Find("figure").GetAttribute("class"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AChartWithALine_TurnsThePackageTooltipsOn_AndOffWhenItGoes()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Tooltip);
        module.SetupVoid("installPackageTooltips").SetVoidResult();
        module.SetupVoid("uninstallPackageTooltips").SetVoidResult();

        var chart = Chart(builder => Child<OmniLineSeries>(builder, 0, ("Data", Points(10, 20))));

        chart.WaitForAssertion(() => Assert.Single(module.Invocations["installPackageTooltips"]));
        chart.Render();
        Assert.Single(module.Invocations["installPackageTooltips"]);
        await DisposeComponentsAsync();
        Assert.Single(module.Invocations["uninstallPackageTooltips"]);
    }

    [Fact]
    public void AChartWithBarsLoadsTheTooltipScript_ButNotOneWithBothOff()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Tooltip);

        var off = Render<OmniChart>(parameters => parameters
            .Add(component => component.Label, "Ventes")
            .Add(component => component.Tooltips, false)
            .Add(component => component.HighlightOnHover, false)
            .Add(component => component.ChildContent, (RenderFragment)(builder => Child<OmniLineSeries>(builder, 0, ("Data", Points(10, 20))))));
        off.WaitForAssertion(() => Assert.Single(off.FindAll("polyline")));
        Assert.Empty(module.Invocations);

        var bars = Chart(builder => Child<OmniColumnSeries>(builder, 0, ("Data", Points(10, 20))));
        bars.WaitForAssertion(() => Assert.NotEmpty(module.Invocations));
    }

    private IRenderedComponent<OmniChart> Bars(OmniChartPoint[] data, bool horizontal, bool contrast = true, int color = 2) =>
        Chart(builder =>
        {
            Child<OmniColumnSeries>(builder, 0, ("Data", data), ("Horizontal", horizontal), ("ColorByPoint", true), ("ColorIndex", color));
            Child<OmniSeriesDataLabels>(builder, 10, ("Data", data), ("Inside", true), ("InsideContrast", contrast));
        });

    [Fact]
    public void ALabelItsBarHolds_TakesTheColourOfThatBar_AndStaysInIt()
    {
        var data = new[] { new OmniChartPoint(0, 80, "Opus · 80%"), new OmniChartPoint(1, 60, "Sonnet · 60%") };
        var chart = Bars(data, horizontal: true);

        chart.WaitForAssertion(() =>
        {
            var labels = chart.FindAll(".omni-chart__labels text");
            Assert.Equal(["omni-chart__label-on-bar omni-chart-color-2", "omni-chart__label-on-bar omni-chart-color-3"], labels.Select(label => label.GetAttribute("class")));
            var start = double.Parse(chart.FindAll(".omni-chart__bars rect")[0].GetAttribute("x")!, CultureInfo.InvariantCulture);
            Assert.All(labels, label => Assert.Equal(start + 1.5, double.Parse(label.GetAttribute("x")!, CultureInfo.InvariantCulture), 3));
        });
    }

    [Fact]
    public void ALabelTooLongForItsBar_IsWrittenPastItsEnd_InTheTextColour()
    {
        var data = new[] { new OmniChartPoint(0, 90, "Opus · 90%"), new OmniChartPoint(1, 2, "Autres · 2%") };
        var chart = Bars(data, horizontal: true);

        chart.WaitForAssertion(() =>
        {
            var labels = chart.FindAll(".omni-chart__labels text");
            Assert.NotNull(labels[0].GetAttribute("class"));
            Assert.Null(labels[1].GetAttribute("class"));
            var bar = chart.FindAll(".omni-chart__bars rect")[1];
            var end = double.Parse(bar.GetAttribute("x")!, CultureInfo.InvariantCulture) + double.Parse(bar.GetAttribute("width")!, CultureInfo.InvariantCulture);
            Assert.Equal(end + 1.5, double.Parse(labels[1].GetAttribute("x")!, CultureInfo.InvariantCulture), 2);
        });
    }

    [Fact]
    public void AColumn_HoldsItsLabelUnderItsTop_AndALabelWiderThanTheColumnGoesAbove()
    {
        var chart = Bars(Points(80, 2), horizontal: false);
        var wide = Bars([new OmniChartPoint(0, 80, new string('x', 80))], horizontal: false);

        chart.WaitForAssertion(() =>
        {
            var labels = chart.FindAll(".omni-chart__labels text");
            var columns = chart.FindAll(".omni-chart__columns rect");
            var top = double.Parse(columns[0].GetAttribute("y")!, CultureInfo.InvariantCulture);
            Assert.NotNull(labels[0].GetAttribute("class"));
            Assert.Equal(top + 1.5, double.Parse(labels[0].GetAttribute("y")!, CultureInfo.InvariantCulture), 2);
            // Too low to hold its text: above the column.
            Assert.Null(labels[1].GetAttribute("class"));
            Assert.True(double.Parse(labels[1].GetAttribute("y")!, CultureInfo.InvariantCulture) < double.Parse(columns[1].GetAttribute("y")!, CultureInfo.InvariantCulture));
        });
        wide.WaitForAssertion(() => Assert.Null(wide.Find(".omni-chart__labels text").GetAttribute("class")));
    }

    [Fact]
    public void WithoutContrast_EveryLabelStaysInside_WithTheOutline()
    {
        var data = new[] { new OmniChartPoint(0, 90, "Opus · 90%"), new OmniChartPoint(1, 2, "Autres · 2%") };
        var chart = Bars(data, horizontal: true, contrast: false);

        chart.WaitForAssertion(() =>
        {
            var labels = chart.FindAll(".omni-chart__labels--inside text");
            Assert.Equal(2, labels.Count);
            Assert.All(labels, label => Assert.Null(label.GetAttribute("class")));
            var start = double.Parse(chart.FindAll(".omni-chart__bars rect")[0].GetAttribute("x")!, CultureInfo.InvariantCulture);
            Assert.All(labels, label => Assert.Equal(start + 1.5, double.Parse(label.GetAttribute("x")!, CultureInfo.InvariantCulture), 3));
        });
    }

    [Fact]
    public void ALabelInsideALine_HasNoBarToFollow_AndKeepsTheOutline()
    {
        var data = Points(10, 20);
        var chart = Chart(builder =>
        {
            Child<OmniLineSeries>(builder, 0, ("Data", data));
            Child<OmniSeriesDataLabels>(builder, 10, ("Data", data), ("Inside", true));
        });

        chart.WaitForAssertion(() => Assert.Equal(2, chart.FindAll(".omni-chart__labels--inside text").Count));
        Assert.All(chart.FindAll(".omni-chart__labels--inside text"), label => Assert.Null(label.GetAttribute("class")));
    }

    [Fact]
    public void TheParts_CarryTheirTextAsATip_AndNoSvgTitle_SoTheBrowserDrawsNoBoxOfItsOwn()
    {
        var chart = Chart(builder =>
        {
            Child<OmniColumnSeries>(builder, 0, ("Title", "Ventes"), ("Data", Points(3, 5)));
            Child<OmniLineSeries>(builder, 10, ("Title", "Cible"), ("Data", Points(4, 6)));
            Child<OmniMarkers>(builder, 20, ("Data", Points(4, 6)));
        });

        chart.WaitForAssertion(() => Assert.NotEmpty(chart.FindAll(".omni-chart__columns rect[data-omni-tip]")));
        Assert.Empty(chart.FindAll("svg title"));
        Assert.False(string.IsNullOrEmpty(chart.Find("svg").GetAttribute("aria-label")));
        Assert.Null(chart.Find("svg").GetAttribute("data-omni-tip"));
    }

    [Fact]
    public void TooltipsOff_LeavesEveryPartWithoutATip_AndKeepsTheHighlight()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Label, "Ventes")
            .Add(component => component.Tooltips, false)
            .Add(component => component.ChildContent, (RenderFragment)(builder =>
            {
                Child<OmniColumnSeries>(builder, 0, ("Data", Points(3, 5)));
                Child<OmniLineSeries>(builder, 10, ("Data", Points(4, 6)));
            })));

        chart.WaitForAssertion(() => Assert.NotEmpty(chart.FindAll(".omni-chart__columns rect")));
        Assert.Empty(chart.FindAll("[data-omni-tip]"));
        Assert.DoesNotContain("omni-chart--no-highlight", chart.Find("figure").GetAttribute("class"), StringComparison.Ordinal);
    }

    [Fact]
    public void TheTooltipOfASlice_WritesItsValueWithFormatValue()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Label, "Origine")
            .AddChildContent<OmniPieSeries>(pie => pie
                .Add(component => component.Data, [new OmniChartSlice("Alpha", 2388.576)])
                .Add(component => component.FormatValue, value => $"{value:0} €")));

        chart.WaitForAssertion(() => Assert.Equal(OmniChartGeometry.Pair("Alpha", $"{2388.576:0} €"), chart.Find("path[data-omni-tip]").GetAttribute("data-omni-tip")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SliceGap_TakesEachSliceAwayFromItsNeighbours_AndZeroLeavesThemTouching(bool donut)
    {
        var whole = OmniChartGeometry.Arc(0, 90, 45, donut);

        Assert.Equal(whole, OmniChartGeometry.Arc(0, 90, 45, donut, 0));
        Assert.NotEqual(whole, OmniChartGeometry.Arc(0, 90, 45, donut, 4));
        Assert.Equal(OmniChartGeometry.Arc(2, 88, 45, false), OmniChartGeometry.Arc(0, 90, 45, false, 4));
    }

    [Fact]
    public void SliceGap_KeepsASmallSliceDrawn_AndASingleSliceWhole()
    {
        // 40 % off each end of a 5 degree slice, whatever the gap asks.
        Assert.Equal(OmniChartGeometry.Arc(2, 3, 45, false), OmniChartGeometry.Arc(0, 5, 45, false, 30));
        Assert.Equal(OmniChartGeometry.Arc(0, 359.999, 45, true), OmniChartGeometry.Arc(0, 359.999, 45, true, 4));
    }

    [Fact]
    public void Stylesheet_ShowsTheHoverOfThePoints_TheBars_AndTheLabelColour()
    {
        var css = StylesheetSource.Read();
        Assert.Contains(".omni-chart__point-zone:hover .omni-chart__point-mark { opacity: 1; }", css, StringComparison.Ordinal);
        Assert.Matches(@"polyline\.omni-chart__point-reach \{[^}]*pointer-events: stroke;[^}]*vector-effect: non-scaling-stroke;", css);
        Assert.Contains(":is(.omni-chart__columns, .omni-chart__bars) :is(rect:hover, rect.omni-chart__part--held)", css, StringComparison.Ordinal);
        Assert.Contains(":is(.omni-chart__pie, .omni-chart__donut) :is(path:hover, path.omni-chart__part--held)", css, StringComparison.Ordinal);
        Assert.Matches(@"\.omni-chart:not\(\.omni-chart--no-highlight\) :is\(\.omni-chart__pie, \.omni-chart__donut\):has\(path:hover, path\.omni-chart__part--held\) path:not\(:hover, \.omni-chart__part--held\) \{ opacity: 0\.6; \}", css);
        Assert.Contains(".omni-chart__labels text { pointer-events: none; }", css, StringComparison.Ordinal);
        Assert.Contains("@supports (color: oklch(from red l c h))", css, StringComparison.Ordinal);
        for (var slot = 0; slot < 8; slot++)
        {
            Assert.Contains($".omni-chart-color-{slot} {{ --omni-chart-part-color: var(--omni-chart-color-{slot});", css, StringComparison.Ordinal);
        }
    }
}
