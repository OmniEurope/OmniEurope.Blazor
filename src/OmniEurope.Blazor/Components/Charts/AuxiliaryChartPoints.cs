
namespace OmniEurope.Blazor.Components;

/// <summary>
/// The points of a chart part that stays out of the legend and the data table (<see cref="OmniMarkers"/>,
/// <see cref="OmniSeriesDataLabels"/>): registers them with the chart around the part so the plot covers them,
/// and projects one of them on that chart or, without a chart, on the range of the points, read once per
/// render rather than once per point.
/// </summary>
internal sealed class AuxiliaryChartPoints
{
    private IReadOnlyList<OmniChartPoint> _data = [];
    private OmniChartGeometry.ChartDomain? _domain;

    /// <summary>Takes the points of the part <paramref name="owner"/> and registers them with its chart, if it has one.</summary>
    internal void Register(object owner, OmniChartContext? chart, IReadOnlyList<OmniChartPoint> data)
    {
        _data = data;
        _domain = null;
        chart?.RegisterSeries(owner, OmniChartSeriesKind.Auxiliary, data);
    }

    /// <summary>Where the point at <paramref name="index"/> is drawn.</summary>
    internal (double X, double Y) Projected(OmniChartContext? chart, int index) =>
        chart is null
            ? OmniChartGeometry.ProjectedPoint(_data, index, _domain ??= OmniChartGeometry.Domains(_data))
            : chart.ProjectCoordinates(_data[index]);
}
