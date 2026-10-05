using OmniEurope.Blazor.Components;
using static OmniEurope.Blazor.Components.OmniChartContext;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The chart context read on its own: every registration added, updated and withdrawn with and without
/// a listener, withdrawals of what was never registered, a legend that takes its entries from a pie,
/// category labels out of reach, an area without points, horizontal projections, a single category,
/// and bounds that cannot be rounded.
/// </summary>
public sealed class ChartContextEdgeTests
{
    private static readonly OmniChartPoint[] Points = [new(0, 1), new(1, 3), new(2, 2)];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Registrations_AddUpdateAndWithdraw_TellingTheirListenerOnlyOfRealChanges(bool listened)
    {
        var context = new OmniChartContext();
        var changes = 0;
        if (listened)
        {
            context.Changed += () => changes++;
        }

        var series = new object();
        var pie = new object();
        var axis = new object();
        var categories = new object();
        var legend = new object();

        context.SetAspectRatio(2);
        context.RegisterSeries(series, OmniChartSeriesKind.Line, Points, "Ventes");
        context.RegisterSeries(series, OmniChartSeriesKind.Line, [new(0, 5)], "Ventes");
        context.RegisterPie(pie, "Parts", [new OmniChartSlice("A", 1)]);
        context.RegisterPie(pie, "Parts", [new OmniChartSlice("A", 2)]);
        context.RegisterValueAxis(axis, 0, 10);
        context.RegisterAutomaticValueAxis(axis, 5);
        context.RegisterCategoryAxis(categories, ["janv.", "févr."]);
        context.RegisterLegend(legend, new LegendRegistration("Légende", [], OmniLegendPosition.Right));
        context.RegisterLegend(legend, new LegendRegistration("Autre", [], OmniLegendPosition.Right));
        var registered = changes;

        foreach (var withdraw in new Action<object>[] { context.UnregisterSeries, context.UnregisterPie, context.UnregisterValueAxis, context.UnregisterCategoryAxis, context.UnregisterLegend })
        {
            withdraw(new object());
        }

        Assert.Equal(registered, changes);
        context.UnregisterSeries(series);
        context.UnregisterPie(pie);
        context.UnregisterValueAxis(axis);
        context.UnregisterCategoryAxis(categories);
        context.UnregisterLegend(legend);

        Assert.Equal(listened ? 10 : 0, registered);
        Assert.Equal(listened ? 15 : 0, changes);
        Assert.Empty(context.TableSeries);
        Assert.Empty(context.Pies);
    }

    [Fact]
    public void Legend_WithoutItsOwnEntries_TakesTheSlicesOfThePie_AndAnUnknownLegendIsNotOutside()
    {
        var context = new OmniChartContext();
        context.RegisterPie(new object(), "Parts", [new OmniChartSlice("Nord", 2), new OmniChartSlice("Sud", 3)]);

        var entries = context.LegendEntries(new LegendRegistration("Légende", [], OmniLegendPosition.Right));

        Assert.Equal(["Nord", "Sud"], entries.Select(entry => entry.Text));
        Assert.False(context.IsLegendOutside(new object()));
    }

    [Fact]
    public void CategoryLabel_WithoutAnAxisOrOutOfItsRange_IsNone()
    {
        var context = new OmniChartContext();
        Assert.Null(context.CategoryLabel(0));

        context.RegisterCategoryAxis(new object(), ["janv."]);

        Assert.Equal("janv.", context.CategoryLabel(0));
        Assert.Null(context.CategoryLabel(-1));
        Assert.Null(context.CategoryLabel(1));
    }

    [Fact]
    public void AreaOfASeriesWithoutPoints_IsEmpty()
    {
        var context = new OmniChartContext();
        var owner = new object();
        context.RegisterSeries(owner, OmniChartSeriesKind.Area, []);

        Assert.Equal(string.Empty, context.AreaPoints(owner, stacked: false));
    }

    [Fact]
    public void HorizontalBars_ProjectValuesAlongTheBottom_AndASingleCategorySitsInTheMiddle()
    {
        var context = new OmniChartContext();
        context.RegisterSeries(new object(), OmniChartSeriesKind.Bar, Points);

        var (x, y) = context.ProjectCoordinates(new OmniChartPoint(1, 3));
        Assert.True(context.Horizontal);
        Assert.Equal(context.PlotRight, x, 3);
        Assert.InRange(y, PlotTop, PlotBottom);

        var line = new OmniChartContext();
        Assert.Equal((line.PlotLeft + line.PlotRight) / 2, line.CategoryPosition(0, 1), 3);
    }

    [Fact]
    public void ManyLongLabels_KeepTheFirstAndTheLast_AndDropTheOneTooCloseToTheLast()
    {
        var context = new OmniChartContext();
        string[] labels = [.. Enumerable.Range(0, 12).Select(index => $"Catégorie numéro {index}")];

        var visible = context.VisibleCategoryIndexes(labels);

        Assert.Equal(0, visible[0]);
        Assert.Equal(11, visible[^1]);
        Assert.True(visible.Count < labels.Length);
        // One label every step, the last one kept in place of the label that came too close to it.
        var step = visible[1] - visible[0];
        Assert.True(step > 1);
        Assert.All(visible.Zip(visible.Skip(1)).SkipLast(1), pair => Assert.Equal(step, pair.Second - pair.First));
        Assert.InRange(visible[^1] - visible[^2], step, (2 * step) - 1);
    }

    [Fact]
    public void LongLabels_ThatFitOnlyAtTheEnds_KeepTheFirstAndTheLast_AndACountOnTheStepKeepsEveryStep()
    {
        var context = new OmniChartContext();
        string Label(int index) => $"Une très longue catégorie {index}";

        Assert.Equal([0, 2], context.VisibleCategoryIndexes([.. Enumerable.Range(0, 3).Select(Label)]));

        // Some count lands its last label exactly one step after the one before: nothing is dropped then.
        var onTheStep = Enumerable.Range(4, 60)
            .Select(count => context.VisibleCategoryIndexes([.. Enumerable.Range(0, count).Select(index => $"Cat {index}")]))
            .First(visible => visible.Count > 2 && visible[1] - visible[0] > 1 && visible[^1] - visible[^2] == visible[1] - visible[0]);
        Assert.All(onTheStep.Zip(onTheStep.Skip(1)), pair => Assert.Equal(onTheStep[1] - onTheStep[0], pair.Second - pair.First));
    }

    [Fact]
    public void BoundsThatAreNotNumbers_AreGivenBackAsTheyCame()
    {
        var (minimum, maximum) = RoundOutward(double.NaN, 1, 5);

        Assert.True(double.IsNaN(minimum));
        Assert.Equal(1, maximum);
    }

    [Fact]
    public void Legends_ThatDifferOnlyByTheirName_AreNotTheSame()
    {
        var first = new LegendRegistration("Ventes", ["A"], OmniLegendPosition.Bottom);

        Assert.False(first.SameAs(first with { Label = "Achats" }));
        Assert.False(first.SameAs(first with { Position = OmniLegendPosition.Top }));
        Assert.True(first.SameAs(new LegendRegistration("Ventes", ["A"], OmniLegendPosition.Bottom)));
    }
}
