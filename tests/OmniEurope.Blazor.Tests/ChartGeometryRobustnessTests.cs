using System.Collections;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The geometry a series draws by itself, without a chart around it: points that are not numbers write no
/// "NaN" in an SVG attribute, and a long series is projected from one pass over its data, not one per point.
/// </summary>
public sealed class ChartGeometryRobustnessTests : OmniBunitContext
{
    private static readonly OmniChartPoint[] WithGaps =
    [
        new(0, 1), new(1, double.NaN), new(2, 3), new(3, double.PositiveInfinity), new(double.NaN, 5), new(4, 2)
    ];

    [Fact]
    public void Points_SkipAPointThatIsNotAFiniteNumber_AndWriteNoNaN()
    {
        var points = OmniChartGeometry.Points(WithGaps);

        Assert.DoesNotContain("NaN", points, StringComparison.Ordinal);
        Assert.DoesNotContain("Infinity", points, StringComparison.Ordinal);
        Assert.Equal(3, points.Split(' ').Length);
    }

    [Fact]
    public void TheDomain_IsTheOneOfTheFinitePoints()
    {
        var finite = OmniChartGeometry.Points([new(0, 1), new(2, 3), new(4, 2)]);

        Assert.Equal(finite, OmniChartGeometry.Points([new(0, 1), new(1, double.NaN), new(2, 3), new(4, 2)]));
    }

    [Fact]
    public void AreaPoints_WriteNoNaN_WhateverThePoints()
    {
        Assert.DoesNotContain("NaN", OmniChartGeometry.AreaPoints(WithGaps), StringComparison.Ordinal);
        Assert.Equal(string.Empty, OmniChartGeometry.AreaPoints([new(double.NaN, double.NaN)]));
    }

    [Fact]
    public void ARectangleOrAPointThatIsNotANumber_SitsOnTheBaselineInsteadOfWritingNaN()
    {
        var column = OmniChartGeometry.ColumnRect(WithGaps, 1);
        var bar = OmniChartGeometry.BarRect(WithGaps, 4);
        var point = OmniChartGeometry.ProjectedPoint(WithGaps, 4);

        foreach (var value in new[] { column.X, column.Y, column.Width, column.Height, bar.X, bar.Y, bar.Width, bar.Height, point.X, point.Y })
        {
            Assert.True(double.IsFinite(value));
        }
    }

    [Fact]
    public void Number_NeverWritesNaNOrInfinity()
    {
        Assert.Equal("0", OmniChartGeometry.Number(double.NaN));
        Assert.Equal("0", OmniChartGeometry.Number(double.PositiveInfinity));
        Assert.Equal("1.5", OmniChartGeometry.Number(1.5));
    }

    [Fact]
    public void ALongSeries_IsProjectedFromAFewPassesOverItsData()
    {
        var data = new CountingList(Enumerable.Range(0, 3_000).Select(index => new OmniChartPoint(index, index % 17)).ToArray());

        var points = OmniChartGeometry.Points(data);
        var area = OmniChartGeometry.AreaPoints(data);

        Assert.Equal(3_000, points.Split(' ').Length);
        Assert.StartsWith(points, area, StringComparison.Ordinal);
        Assert.True(data.Enumerations <= 12, $"{data.Enumerations} passes over the data");
        Assert.True(data.Reads <= 10 * 3_000, $"{data.Reads} reads of the data");
    }

    [Fact]
    public void TheSeriesComponents_ProjectALongSeriesFromOnePassOverTheirData()
    {
        var data = new CountingList(Enumerable.Range(0, 1_500).Select(index => new OmniChartPoint(index, index % 17)).ToArray());

        Render<OmniMarkers>(parameters => parameters.Add(component => component.Data, data));
        var markerReads = data.Reads;
        Render<OmniColumnSeries>(parameters => parameters.Add(component => component.Data, data));

        Assert.True(markerReads <= 40 * 1_500, $"{markerReads} reads of the data for the markers");
        Assert.True(data.Reads - markerReads <= 40 * 1_500, $"{data.Reads - markerReads} reads of the data for the columns");
    }

    private sealed class CountingList(OmniChartPoint[] items) : IReadOnlyList<OmniChartPoint>
    {
        public int Enumerations { get; private set; }

        public int Reads { get; private set; }

        public int Count => items.Length;

        public OmniChartPoint this[int index]
        {
            get
            {
                Reads++;
                return items[index];
            }
        }

        public IEnumerator<OmniChartPoint> GetEnumerator()
        {
            Enumerations++;
            foreach (var item in items)
            {
                Reads++;
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void AreaPoints_CloseTheShapeOnTheFirstAndLastFinitePoints()
    {
        OmniChartPoint[] points = [new(double.NaN, 1), new(0, 1), new(4, 3), new(5, double.NaN)];

        var area = OmniChartGeometry.AreaPoints(points);

        Assert.Equal(OmniChartGeometry.AreaPoints([new(0, 1), new(4, 3)]), area);
    }

    [Fact]
    public void TheDataLabels_WithoutAChart_ProjectEachPointOverTheSameRange()
    {
        var labels = Render<OmniSeriesDataLabels>(parameters => parameters
            .Add(component => component.Data, new OmniChartPoint[] { new(0, 1), new(1, 3), new(2, 2) }));

        Assert.NotEmpty(labels.FindAll("text"));
        Assert.DoesNotContain("NaN", labels.Markup, StringComparison.Ordinal);
    }
}
