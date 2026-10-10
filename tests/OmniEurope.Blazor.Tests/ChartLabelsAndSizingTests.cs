using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Requests of a client application's recette (R1-12 to R1-16): data labels that would overlap are
/// masked as the category axis thins its labels, the range navigator is graduated, a pie writes its
/// labels outside, the chart keeps a minimum width, and the stat tile keeps its label on one line.
/// What only a browser measures (the grip of the handles, the fitted label, the scroll) is proved by
/// the scripts probe (eng/scripts-probe/requests.mjs).
/// </summary>
public sealed class ChartLabelsAndSizingTests : OmniBunitContext
{
    private static readonly string[] Months =
        [.. Enumerable.Range(0, 36).Select(month => new DateOnly(2024, 1, 1).AddMonths(month).ToString("MMM yyyy", CultureInfo.InvariantCulture))];

    private static IReadOnlyList<OmniChartPoint> Points(int count, double value = 123456.78) =>
        [.. Enumerable.Range(0, count).Select(index => new OmniChartPoint(index + 1, value + index))];

    private static DataLabelThinning.Box? At(double left, double width = 10) => new DataLabelThinning.Box(left, left + width, 0, 3);

    [Fact]
    public void Thinning_MasksALabelThatOverlapsThePreviousOneKept()
    {
        var visible = DataLabelThinning.Keep([At(0), At(5), At(11), At(16), At(22)]);

        Assert.Equal([true, false, true, false, true], visible);
    }

    [Fact]
    public void Thinning_AlwaysKeepsTheLast_AndMasksTheKeptOneBeforeItWhenTheyOverlap()
    {
        // Kept: 0, 11; the last (at 15) overlaps 11, so 11 gives way and the last is drawn.
        var visible = DataLabelThinning.Keep([At(0), At(11), At(15)]);

        Assert.Equal([true, false, true], visible);
    }

    [Fact]
    public void Thinning_LeavesLabelsAtDifferentHeightsAlone_AndIgnoresPointsOutOfTheRange()
    {
        var high = new DataLabelThinning.Box(5, 15, 20, 23);
        var visible = DataLabelThinning.Keep([At(0), high, null, At(30)]);

        Assert.Equal([true, true, true, true], visible);
    }

    [Fact]
    public void DataLabels_OfManyPoints_DoNotOverlap_TheLastIsKept_AndAMaskedValueIsReadOnHover()
    {
        var data = Points(36);
        var chart = RenderChart(builder =>
        {
            builder.OpenComponent<OmniCategoryAxis>(0);
            builder.AddComponentParameter(1, nameof(OmniCategoryAxis.Labels), Months);
            builder.CloseComponent();
            builder.OpenComponent<OmniLineSeries>(2);
            builder.AddComponentParameter(3, nameof(OmniLineSeries.Data), data);
            builder.CloseComponent();
            builder.OpenComponent<OmniSeriesDataLabels>(4);
            builder.AddComponentParameter(5, nameof(OmniSeriesDataLabels.Data), data);
            builder.CloseComponent();
        });

        var texts = chart.FindAll(".omni-chart__labels text");
        var hits = chart.FindAll(".omni-chart__labels .omni-chart__label-hit title");
        Assert.InRange(texts.Count, 2, 35);
        Assert.Equal(36, texts.Count + hits.Count);
        Assert.Equal(OmniChartGeometry.Display(data[^1].Y), texts[^1].TextContent);

        // No two labels drawn overlap: each starts after the end of the previous one (their x, centred, and
        // their estimated width, the estimate the component uses).
        var extents = texts
            .Select(text => (X: double.Parse(text.GetAttribute("x")!, CultureInfo.InvariantCulture), Width: text.TextContent.Length * 1.7, Anchor: text.GetAttribute("text-anchor")))
            .Select(label => label.Anchor switch
            {
                "middle" => (Left: label.X - (label.Width / 2), Right: label.X + (label.Width / 2)),
                "end" => (Left: label.X - label.Width, Right: label.X),
                _ => (Left: label.X, Right: label.X + label.Width)
            })
            .ToArray();
        for (var index = 1; index < extents.Length; index++)
        {
            Assert.True(extents[index].Left >= extents[index - 1].Right, $"Label {index} overlaps the previous one.");
        }

        Assert.All(hits, hit => Assert.False(string.IsNullOrWhiteSpace(hit.TextContent)));
    }

    [Fact]
    public void RangeNavigator_IsGraduated_ThinnedLikeTheCategoryAxis_FirstAndLastKept()
    {
        var data = Points(36, 10);
        var chart = RenderChart(builder =>
        {
            builder.OpenComponent<OmniCategoryAxis>(0);
            builder.AddComponentParameter(1, nameof(OmniCategoryAxis.Labels), Months);
            builder.CloseComponent();
            builder.OpenComponent<OmniColumnSeries>(2);
            builder.AddComponentParameter(3, nameof(OmniColumnSeries.Data), data);
            builder.CloseComponent();
        }, footer: builder =>
        {
            builder.OpenComponent<OmniRangeNavigator>(0);
            builder.CloseComponent();
        });

        var ticks = chart.FindAll(".omni-range-navigator__ticks text").Select(text => text.TextContent).ToArray();
        Assert.InRange(ticks.Length, 3, 35);
        Assert.Equal(Months[0], ticks[0]);
        Assert.Equal(Months[^1], ticks[^1]);

        // Regular: the same step between every graduation but the last.
        var indexes = ticks.Select(tick => Array.IndexOf(Months, tick)).ToArray();
        var steps = indexes.Zip(indexes.Skip(1), (left, right) => right - left).ToArray();
        Assert.All(steps[..^1], step => Assert.Equal(steps[0], step));
        Assert.Equal(ticks.Length, chart.FindAll(".omni-range-navigator__ticks line").Count);
    }

    [Theory]
    [InlineData(OmniPieLabels.Name, "Belgique")]
    [InlineData(OmniPieLabels.NameAndPercent, "Belgique 50")]
    [InlineData(OmniPieLabels.NameAndValue, "Belgique 6")]
    public void Pie_OutsideLabels_WriteEachSliceBesideTheDisc_JoinedByALeaderLine(OmniPieLabels labels, string first)
    {
        IReadOnlyList<OmniChartSlice> slices =
        [
            new("Belgique", 6), new("France", 2), new("Luxembourg", 1), new("Pays-Bas", 1), new("Allemagne", 1), new("Espagne", 1)
        ];
        var chart = RenderChart(builder =>
        {
            builder.OpenComponent<OmniPieSeries>(0);
            builder.AddComponentParameter(1, nameof(OmniPieSeries.Data), slices);
            builder.AddComponentParameter(2, nameof(OmniPieSeries.OutsideLabels), labels);
            builder.CloseComponent();
        }, aspectRatio: 1.6);

        var texts = chart.FindAll(".omni-chart__pie-labels text");
        Assert.Equal(slices.Count, texts.Count);
        Assert.Equal(slices.Count, chart.FindAll(".omni-chart__pie-labels polyline").Count);
        Assert.StartsWith(first, texts[0].TextContent, StringComparison.Ordinal);

        // Each side stacked: the labels of one side are at least a line apart, inside the drawing.
        foreach (var side in texts.GroupBy(text => text.GetAttribute("text-anchor") == "end"))
        {
            var heights = side.Select(text => double.Parse(text.GetAttribute("y")!, CultureInfo.InvariantCulture)).Order().ToArray();
            Assert.All(heights, height => Assert.InRange(height, 3, 99));
            for (var index = 1; index < heights.Length; index++)
            {
                Assert.True(heights[index] - heights[index - 1] >= 3.75 - 0.001, $"Two labels of one side are {heights[index] - heights[index - 1]} apart.");
            }
        }
    }

    [Fact]
    public void Pie_WithoutOutsideLabels_KeepsItsFullDiscAndWritesNoLabel()
    {
        IReadOnlyList<OmniChartSlice> slices = [new("A", 1), new("B", 1)];
        var chart = RenderChart(builder =>
        {
            builder.OpenComponent<OmniPieSeries>(0);
            builder.AddComponentParameter(1, nameof(OmniPieSeries.Data), slices);
            builder.CloseComponent();
        });

        Assert.Empty(chart.FindAll(".omni-chart__pie-labels"));
        Assert.Equal(OmniChartGeometry.Arc(0, 179.9995, 42, false), chart.Find(".omni-chart__pie path").GetAttribute("d"));
    }

    [Fact]
    public void PieLabelStack_PushesCrowdedLabelsApart_AndBackUpAtTheBottom()
    {
        var heights = PieLabelLayout.Stack([50, 50, 50, 98, 98]);

        for (var index = 1; index < heights.Length; index++)
        {
            Assert.True(heights[index] - heights[index - 1] >= 3.75 - 0.001);
        }

        Assert.True(heights[^1] <= 99);
    }

    [Theory]
    [InlineData(1, OmniChartMinWidth.Auto, "omni-chart--min-small")]
    [InlineData(1.6, OmniChartMinWidth.Auto, "omni-chart--min-medium")]
    [InlineData(2, OmniChartMinWidth.Auto, "omni-chart--min-large")]
    [InlineData(3, OmniChartMinWidth.Auto, "omni-chart--min-xlarge")]
    [InlineData(4, OmniChartMinWidth.Auto, "omni-chart--min-widest")]
    [InlineData(4, OmniChartMinWidth.None, null)]
    public void Chart_MinWidth_ComesFromItsShape_OrIsTheOneGiven(double aspectRatio, OmniChartMinWidth minWidth, string? expected)
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Soldes")
            .Add(component => component.AspectRatio, aspectRatio)
            .Add(component => component.MinWidth, minWidth));

        var classes = chart.Find("figure.omni-chart").ClassList.Where(name => name.StartsWith("omni-chart--min-", StringComparison.Ordinal)).ToArray();
        if (expected is null) Assert.Empty(classes);
        else Assert.Equal([expected], classes);
    }

    [Fact]
    public void StatTile_AttachesTheScriptThatKeepsItsLabelOnOneLine()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.StatTile);

        var tile = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Value, "1 284 €")
            .Add(component => component.Label, "Plus-value des placements"));

        tile.WaitForAssertion(() => Assert.Single(module.Invocations["attach"]));
        Assert.Equal("Plus-value des placements", tile.Find(".omni-stat-tile__label").TextContent);
    }

    [Fact]
    public async Task StatTile_AttachesOnce_AndDetachesItsScriptWhenDisposed()
    {
        var runtime = new ManualJSRuntime();
        Services.AddSingleton<IJSRuntime>(runtime);
        var tile = Render<OmniStatTile>(parameters => parameters.Add(component => component.Label, "Jetons"));
        tile.WaitForAssertion(() => Assert.Equal(["attach"], runtime.Module.Calls));

        tile.Render(parameters => parameters.Add(component => component.Value, "12,4 k"));
        await tile.Instance.DisposeAsync();

        Assert.Equal(["attach", "detach"], runtime.Module.Calls);
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public async Task StatTile_DisposedBeforeItsScriptLoads_ReleasesTheModuleWithoutAttaching()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var tile = Render<OmniStatTile>(parameters => parameters.Add(component => component.Label, "Jetons"));

        await tile.Instance.DisposeAsync();
        runtime.PendingImport.SetResult(runtime.Module);

        await runtime.Module.Disposal.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public async Task StatTile_ImportedOnALostCircuit_IsQuiet()
    {
        var lost = new ManualJSRuntime { ImportFailure = new JSDisconnectedException("perdu") };
        Services.AddSingleton<IJSRuntime>(lost);
        var tile = Render<OmniStatTile>(parameters => parameters.Add(component => component.Label, "Jetons"));
        await tile.Instance.DisposeAsync();

        Assert.Empty(lost.Module.Calls);
        Assert.Equal("Jetons", tile.Find(".omni-stat-tile__label").TextContent);
    }

    [Fact]
    public async Task StatTile_DetachedOnALostCircuit_IsQuiet()
    {
        var detached = new ManualJSRuntime();
        detached.Module.CallFailures["detach"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(detached);
        var second = Render<OmniStatTile>(parameters => parameters.Add(component => component.Label, "Jetons"));
        second.WaitForAssertion(() => Assert.Equal(["attach"], detached.Module.Calls));

        await second.Instance.DisposeAsync();

        Assert.Equal(["attach", "detach"], detached.Module.Calls);
    }

    [Fact]
    public void Thinning_WithoutAnyBox_DrawsEveryLabel() =>
        Assert.Equal([true, true], DataLabelThinning.Keep([null, null]));

    [Fact]
    public void Thinning_KeepsTheLastBox_BeforePointsOutOfTheRange_EvenWhenEveryOtherGivesWay()
    {
        // The last box is the second one (the third point is out of the range): the first gives way to it.
        var visible = DataLabelThinning.Keep([At(0), At(5), null]);

        Assert.Equal([false, true, true], visible);
    }

    [Theory]
    [InlineData("middle", "hanging", 7, 20)]
    [InlineData("end", "central", 4, 18.5)]
    [InlineData(null, null, 10, 17)]
    public void Thinning_BoxOf_FollowsTheAnchorAndTheBaseline(string? anchor, string? baseline, double left, double top)
    {
        var box = DataLabelThinning.BoxOf(10, 20, anchor, baseline, 6, 3);

        Assert.Equal(new DataLabelThinning.Box(left, left + 6, top, top + 3), box);
    }

    [Fact]
    public void DataLabels_Inside_OnAVerticalChart_HangUnderTheTopOfEachColumn()
    {
        IReadOnlyList<OmniChartPoint> data = [new(0, 10), new(1, 20), new(2, 30)];
        var chart = RenderChart(builder =>
        {
            builder.OpenComponent<OmniColumnSeries>(0);
            builder.AddComponentParameter(1, nameof(OmniColumnSeries.Data), data);
            builder.CloseComponent();
            builder.OpenComponent<OmniSeriesDataLabels>(2);
            builder.AddComponentParameter(3, nameof(OmniSeriesDataLabels.Data), data);
            builder.AddComponentParameter(4, nameof(OmniSeriesDataLabels.Inside), true);
            builder.CloseComponent();
        });

        chart.WaitForAssertion(() =>
        {
            var columns = chart.FindAll(".omni-chart__columns rect");
            var labels = chart.FindAll(".omni-chart__labels--inside text");
            Assert.Equal(3, labels.Count);
            for (var index = 0; index < labels.Count; index++)
            {
                Assert.Equal("hanging", labels[index].GetAttribute("dominant-baseline"));
                Assert.Equal("middle", labels[index].GetAttribute("text-anchor"));
                var top = double.Parse(columns[index].GetAttribute("y")!, CultureInfo.InvariantCulture);
                Assert.Equal(top + 1.5, double.Parse(labels[index].GetAttribute("y")!, CultureInfo.InvariantCulture), 3);
            }
        });
    }

    [Fact]
    public void Chart_WithAnId_NamesTheClipOfItsRangeAfterIt()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Id, "soldes")
            .Add(component => component.Title, "Soldes")
            .Add(component => component.ChildContent, builder =>
            {
                builder.OpenComponent<OmniColumnSeries>(0);
                builder.AddComponentParameter(1, nameof(OmniColumnSeries.Data), Points(6, 10));
                builder.CloseComponent();
            })
            .Add(component => component.FooterContent, builder =>
            {
                builder.OpenComponent<OmniRangeNavigator>(0);
                builder.AddComponentParameter(1, nameof(OmniRangeNavigator.RangeStart), (int?)2);
                builder.CloseComponent();
            }));

        chart.WaitForAssertion(() =>
        {
            Assert.Equal("soldes-clip", chart.Find("clipPath").GetAttribute("id"));
            Assert.Equal("url(#soldes-clip)", chart.Find(".omni-chart__parts").GetAttribute("clip-path"));
        });
    }

    [Fact]
    public void Pie_OutsideAChart_LaysOutItsLabels_AndShortensOneTooLong_KeepingItWholeOnHover()
    {
        const string Long = "Une étiquette bien trop longue pour tenir à côté du disque";
        IReadOnlyList<OmniChartSlice> slices = [new(Long, 1), new("B", 1)];

        var pie = Render<OmniPieSeries>(parameters => parameters
            .Add(component => component.Data, slices)
            .Add(component => component.OutsideLabels, OmniPieLabels.Name));

        var texts = pie.FindAll(".omni-chart__pie-labels text");
        Assert.Equal(2, texts.Count);
        Assert.EndsWith("…", texts[0].FirstChild!.TextContent, StringComparison.Ordinal);
        Assert.StartsWith("Une étiquett", texts[0].FirstChild!.TextContent, StringComparison.Ordinal);
        Assert.Equal(Long, texts[0].QuerySelector("title")!.TextContent);
        Assert.Equal("B", texts[1].TextContent);
        Assert.Null(texts[1].QuerySelector("title"));
    }

    [Fact]
    public void PieLabelRadius_WithoutLabels_LeavesRoomForTheLeaderLinesOnly() =>
        Assert.Equal(PieLabelLayout.Radius([string.Empty], 50), PieLabelLayout.Radius([], 50));

    private IRenderedComponent<OmniChart> RenderChart(RenderFragment parts, RenderFragment? footer = null, double aspectRatio = 4) =>
        Render<OmniChart>(parameters =>
        {
            parameters
                .Add(component => component.Title, "Soldes")
                .Add(component => component.AspectRatio, aspectRatio)
                .Add(component => component.ChildContent, parts);
            if (footer is not null) parameters.Add(component => component.FooterContent, footer);
        });
}
