using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The range navigator under a chart (OmniRangeNavigator, recette R-034): the initial range is the one
/// given, a moved handle raises its binding and redraws the chart on the range alone, the handles never
/// cross and stay within the categories. A handle is a native range input: the browser turns the
/// pointer, a finger and the slider keys into its <c>input</c> event, which is what these tests raise.
/// </summary>
public sealed class ChartRangeNavigatorTests : OmniBunitContext
{
    private IRenderedComponent<ChartRangeNavigatorTestHost> Host(int? start, int? end) =>
        Render<ChartRangeNavigatorTestHost>(parameters => parameters
            .Add(host => host.Start, start)
            .Add(host => host.End, end));

    private static IElement StartHandle(IRenderedComponent<ChartRangeNavigatorTestHost> host) =>
        host.Find("input.omni-range-navigator__handle--start");

    private static IElement EndHandle(IRenderedComponent<ChartRangeNavigatorTestHost> host) =>
        host.Find("input.omni-range-navigator__handle--end");

    private static string[] AxisLabels(IRenderedComponent<ChartRangeNavigatorTestHost> host) =>
        [.. host.FindAll(".omni-chart__axis--category text").Select(text => text.TextContent)];

    private static string[] TableRows(IRenderedComponent<ChartRangeNavigatorTestHost> host) =>
        [.. host.FindAll(".omni-chart__table tbody th").Select(cell => cell.TextContent)];

    private static double TopTick(IRenderedComponent<ChartRangeNavigatorTestHost> host) =>
        host.FindAll(".omni-chart__axis--value text")
            .Select(text => double.Parse(text.TextContent.Replace(" ", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal), NumberStyles.Number, CultureInfo.CurrentCulture))
            .Max();

    [Fact]
    public void TheInitialRange_IsTheOneGiven_OnTheHandlesAndInTheChart()
    {
        var host = Host(1, 3);

        host.WaitForAssertion(() =>
        {
            Assert.Equal("1", StartHandle(host).GetAttribute("value"));
            Assert.Equal("3", EndHandle(host).GetAttribute("value"));
            Assert.Equal(["Fév", "Mar", "Avr"], AxisLabels(host));
            Assert.Equal(["Fév", "Mar", "Avr"], TableRows(host));
        });

        // Every handle is a slider over every category, named and valued by the category it stands on.
        var start = StartHandle(host);
        Assert.Equal("range", start.GetAttribute("type"));
        Assert.Equal("slider", start.GetAttribute("role"));
        Assert.Equal("0", start.GetAttribute("aria-valuemin"));
        Assert.Equal("5", start.GetAttribute("aria-valuemax"));
        Assert.Equal("1", start.GetAttribute("aria-valuenow"));
        Assert.Equal("Fév", start.GetAttribute("aria-valuetext"));
        Assert.Equal("Début de la plage", start.GetAttribute("aria-label"));
        Assert.Equal("Fin de la plage", EndHandle(host).GetAttribute("aria-label"));
        Assert.Equal("Avr", EndHandle(host).GetAttribute("aria-valuetext"));
        Assert.Equal("Plage affichée", host.Find(".omni-range-navigator").GetAttribute("aria-label"));

        // The overview keeps every category, the ones of the range set apart.
        Assert.Equal(6, host.FindAll(".omni-range-navigator__column").Count);
        Assert.Equal(3, host.FindAll(".omni-range-navigator__column--selected").Count);
        Assert.Equal("true", host.Find(".omni-range-navigator__overview").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void WithoutAGivenRange_EveryCategoryIsShown_AndNoClipIsSet()
    {
        var host = Host(null, null);

        host.WaitForAssertion(() => Assert.Equal("5", EndHandle(host).GetAttribute("value")));
        Assert.Equal("0", StartHandle(host).GetAttribute("value"));
        Assert.Equal(6, host.FindAll(".omni-chart__columns rect").Count);
        Assert.Equal(6, TableRows(host).Length);
        Assert.Null(host.Find(".omni-chart__parts").GetAttribute("clip-path"));
        Assert.Empty(host.FindAll("clipPath"));
    }

    [Fact]
    public void TheChart_DrawsOnlyThePointsOfTheRange_AndFitsItsValueAxisToThem()
    {
        var host = Host(0, 5);
        host.WaitForAssertion(() => Assert.Equal(6, host.FindAll(".omni-chart__columns rect").Count));
        Assert.True(TopTick(host) >= 600);

        host.Render(parameters => parameters.Add(component => component.Start, 1).Add(component => component.End, 3));

        host.WaitForAssertion(() =>
        {
            Assert.Equal(3, host.FindAll(".omni-chart__columns rect").Count);
            Assert.Equal(3, host.Find("polyline.omni-chart__line").GetAttribute("points")!.Split(' ').Length);
            Assert.Equal(3, host.FindAll(".omni-chart__markers circle").Count);
            Assert.Equal(["Fév", "Mar", "Avr"], host.FindAll(".omni-chart__hover-band title").Select(title => title.TextContent.Split('\n')[0]));
            Assert.Equal(["Mar · 30", "Avr · 40"], host.FindAll(".omni-chart__columns rect title").Skip(1).Select(title => title.TextContent));
            // The June value of 600 is out of the range: the axis fits the 40 of April.
            Assert.True(TopTick(host) < 100);
        });

        // The three columns share the plot: the first starts near its left edge, the last ends near its right one.
        var rectangles = host.FindAll(".omni-chart__columns rect")
            .Select(rect => (X: Number(rect, "x"), Width: Number(rect, "width")))
            .ToArray();
        Assert.True(rectangles[0].X < 30, $"first column at {rectangles[0].X}");
        Assert.True(rectangles[2].X + rectangles[2].Width > 80, $"last column ends at {rectangles[2].X + rectangles[2].Width}");

        // Data labels walk every point of their own: those out of the range land outside the clip of the parts.
        var clip = host.Find(".omni-chart__parts").GetAttribute("clip-path");
        Assert.Matches("^url\\(#.+-clip\\)$", clip);
        var labels = host.FindAll(".omni-chart__labels text").Select(text => Number(text, "y")).ToArray();
        Assert.Equal(3, labels.Count(y => y is > -10 and < 110));
        Assert.Equal(3, labels.Count(y => y < -10));
    }

    [Fact]
    public void AMovedHandle_RaisesItsBinding_AndRedrawsTheChart()
    {
        var host = Host(1, 3);
        host.WaitForAssertion(() => Assert.Equal(3, host.FindAll(".omni-chart__columns rect").Count));

        // What the browser raises for an arrow key on the start handle: one category to the left.
        StartHandle(host).Input("0");

        host.WaitForAssertion(() =>
        {
            Assert.Equal([0], host.Instance.StartsRaised);
            Assert.Equal(["Jan", "Fév", "Mar", "Avr"], AxisLabels(host));
            Assert.Equal(4, host.FindAll(".omni-chart__columns rect").Count);
        });

        // End, on the end handle, goes to the last category.
        EndHandle(host).Input("5");

        host.WaitForAssertion(() =>
        {
            Assert.Equal([5], host.Instance.EndsRaised);
            Assert.Equal(6, host.FindAll(".omni-chart__columns rect").Count);
            Assert.Equal("Juin", EndHandle(host).GetAttribute("aria-valuetext"));
        });
    }

    [Fact]
    public void TheHandles_NeverCross()
    {
        var host = Host(1, 3);
        host.WaitForAssertion(() => Assert.Equal("3", EndHandle(host).GetAttribute("value")));

        StartHandle(host).Input("5");

        host.WaitForAssertion(() =>
        {
            Assert.Equal([3], host.Instance.StartsRaised);
            Assert.Equal("3", StartHandle(host).GetAttribute("value"));
            Assert.Equal(["Avr"], AxisLabels(host));
            Assert.Single(host.FindAll(".omni-chart__columns rect"));
        });

        EndHandle(host).Input("0");

        host.WaitForAssertion(() =>
        {
            Assert.Equal([3], host.Instance.EndsRaised);
            Assert.Equal("3", EndHandle(host).GetAttribute("value"));
            Assert.Single(host.FindAll(".omni-chart__columns rect"));
        });

        // Both handles on April, right of the middle: the start handle comes above the end one, which cannot go further right.
        Assert.Contains("omni-range-navigator--start-on-top", host.Find(".omni-range-navigator").ClassList);
    }

    [Fact]
    public void TheRange_IsHeldWithinTheCategories()
    {
        var host = Host(-4, 40);

        host.WaitForAssertion(() =>
        {
            Assert.Equal("0", StartHandle(host).GetAttribute("value"));
            Assert.Equal("5", EndHandle(host).GetAttribute("value"));
            Assert.Equal(6, host.FindAll(".omni-chart__columns rect").Count);
        });

        StartHandle(host).Input("-2");
        host.WaitForAssertion(() => Assert.Equal([0], host.Instance.StartsRaised));

        EndHandle(host).Input("12");
        host.WaitForAssertion(() =>
        {
            Assert.Equal([5], host.Instance.EndsRaised);
            Assert.Equal("5", EndHandle(host).GetAttribute("value"));
        });
    }

    [Fact]
    public void ANavigatorWrittenInTheDrawing_IsRefused()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Dépôts")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniColumnSeries>(0);
                builder.AddAttribute(1, nameof(OmniColumnSeries.Data), ChartRangeNavigatorTestHost.Deposits);
                builder.CloseComponent();
                builder.OpenComponent<OmniRangeNavigator>(2);
                builder.CloseComponent();
            })));

        Assert.Contains("FooterContent", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RemovingTheNavigator_ShowsEveryCategoryAgain()
    {
        var showNavigator = true;
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Dépôts")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniColumnSeries>(0);
                builder.AddAttribute(1, nameof(OmniColumnSeries.Data), ChartRangeNavigatorTestHost.Deposits);
                builder.CloseComponent();
            })
            .Add(component => component.FooterContent, builder =>
            {
                if (showNavigator)
                {
                    builder.OpenComponent<OmniRangeNavigator>(0);
                    builder.AddAttribute(1, nameof(OmniRangeNavigator.RangeStart), (int?)4);
                    builder.CloseComponent();
                }
            }));
        chart.WaitForAssertion(() => Assert.Equal(2, chart.FindAll(".omni-chart__columns rect").Count));

        showNavigator = false;
        chart.Render();

        chart.WaitForAssertion(() =>
        {
            Assert.Empty(chart.FindAll(".omni-range-navigator"));
            Assert.Equal(6, chart.FindAll(".omni-chart__columns rect").Count);
        });
    }

    [Fact]
    public void TheRangeOfAChart_ReportsAChange_OnlyWhenItsOwnerOrOneOfItsBoundsChanged()
    {
        var range = new OmniChartRange();
        object first = new(), second = new();

        Assert.True(range.Set(first, 1, 3));
        Assert.False(range.Set(first, 1, 3));
        Assert.True(range.Set(first, 1, 4));
        Assert.True(range.Set(first, 2, 4));
        Assert.True(range.Set(second, 2, 4));
        Assert.False(range.Clear(first));
        Assert.True(range.Clear(second));
        Assert.Null(range.Window);
    }

    [Fact]
    public void ANavigatorOutsideAChart_DrawsNothing()
    {
        var navigator = Render<OmniRangeNavigator>(parameters => parameters.Add(component => component.RangeStart, 1));

        Assert.Empty(navigator.Markup.Trim());
        Assert.Empty(navigator.Instance.Ticks());
        navigator.Instance.Dispose();
    }

    [Fact]
    public void AChartWithoutCategories_DrawsNoNavigator_AndShowsNoRange()
    {
        var chart = ChartWith(_ => { }, start: 1);

        Assert.Empty(chart.FindAll(".omni-range-navigator"));
        Assert.Empty(chart.FindAll("clipPath"));
    }

    [Fact]
    public void ASingleCategory_IsGraduatedInTheMiddle_AndShowsNoRange()
    {
        var chart = ChartWith(builder => Series<OmniColumnSeries>(builder, [new(0, 10)]));

        chart.WaitForAssertion(() => Assert.Single(chart.FindAll(".omni-range-navigator")));
        var width = Number(chart.Find(".omni-range-navigator__ticks").GetAttribute("viewBox")!.Split(' ')[2]);
        var tick = Assert.Single(chart.FindAll(".omni-range-navigator__ticks line"));
        Assert.Equal(width / 2, Number(tick, "x1"), 3);
        Assert.Empty(chart.FindAll("clipPath"));
    }

    [Fact]
    public void CategoriesThatAllTotalZero_DrawFlatColumnsOnTheBaseline()
    {
        var chart = ChartWith(builder => Series<OmniColumnSeries>(builder, [new(0, 0), new(1, 0), new(2, 0)]), start: 1);

        chart.WaitForAssertion(() => Assert.Equal(3, chart.FindAll(".omni-range-navigator__column").Count));
        Assert.All(chart.FindAll(".omni-range-navigator__column"), column =>
        {
            Assert.Equal(0, Number(column, "height"));
            Assert.Equal(100, Number(column, "y"));
        });
    }

    [Fact]
    public void ACategoryWithoutAxisLabel_IsNamedByItsPoint_OrElseByItsRank()
    {
        string[] labels = ["Jan", null!, "Mar"];
        void Axis(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<OmniCategoryAxis>(10);
            builder.AddComponentParameter(11, nameof(OmniCategoryAxis.Labels), labels);
            builder.CloseComponent();
        }

        var withPoints = ChartWith(builder =>
        {
            Axis(builder);
            Series<OmniColumnSeries>(builder, [new(7, 1), new(8, 2), new(9, 3)]);
        }, start: 1, end: 1);
        var withoutPoints = ChartWith(Axis, start: 1, end: 1);

        withPoints.WaitForAssertion(() => Assert.Equal("8", withPoints.Find("input.omni-range-navigator__handle--start").GetAttribute("aria-valuetext")));
        withoutPoints.WaitForAssertion(() => Assert.Equal("2", withoutPoints.Find("input.omni-range-navigator__handle--start").GetAttribute("aria-valuetext")));
        Assert.Equal("Jan", withoutPoints.Find(".omni-range-navigator__ticks text").TextContent);
    }

    [Fact]
    public void AStackedArea_DrawsOnlyThePointsOfTheRange()
    {
        var chart = ChartWith(builder => Series<OmniAreaSeries>(builder, ChartRangeNavigatorTestHost.Deposits, stacked: true), start: 1, end: 3);

        chart.WaitForAssertion(() =>
        {
            // Three points on top and the three of their baseline, back to the start.
            var points = chart.Find(".omni-chart__parts polygon").GetAttribute("points")!.Split(' ');
            Assert.Equal(6, points.Length);
        });
        Assert.True(TopTick(chart) < 600);
    }

    private IRenderedComponent<OmniChart> ChartWith(Microsoft.AspNetCore.Components.RenderFragment parts, int? start = null, int? end = null) =>
        Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Dépôts")
            .Add(component => component.ChildContent, builder =>
            {
                parts(builder);
                builder.OpenComponent<OmniValueAxis>(20);
                builder.AddComponentParameter(21, nameof(OmniValueAxis.Automatic), true);
                builder.CloseComponent();
            })
            .Add(component => component.FooterContent, builder =>
            {
                builder.OpenComponent<OmniRangeNavigator>(0);
                builder.AddComponentParameter(1, nameof(OmniRangeNavigator.RangeStart), start);
                builder.AddComponentParameter(2, nameof(OmniRangeNavigator.RangeEnd), end);
                builder.CloseComponent();
            }));

    private static void Series<TSeries>(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, IReadOnlyList<OmniChartPoint> data, bool stacked = false)
        where TSeries : Microsoft.AspNetCore.Components.IComponent
    {
        builder.OpenComponent<TSeries>(0);
        builder.AddComponentParameter(1, "Data", data);
        if (stacked) builder.AddComponentParameter(2, "Stacked", true);
        builder.CloseComponent();
    }

    private static double TopTick(IRenderedComponent<OmniChart> chart) =>
        chart.FindAll(".omni-chart__axis--value text")
            .Select(text => double.Parse(text.TextContent.Replace(" ", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal), NumberStyles.Number, CultureInfo.CurrentCulture))
            .Max();

    private static double Number(string value) => double.Parse(value, CultureInfo.InvariantCulture);

    private static double Number(IElement element, string attribute) =>
        double.Parse(element.GetAttribute(attribute)!, CultureInfo.InvariantCulture);
}
