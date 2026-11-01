using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The hover of the lines of an <see cref="OmniChart"/> with <see cref="OmniChart.HighlightOnHover"/>: each
/// line is cut into one transparent zone per point, from halfway to the previous point to halfway to the
/// next one, so anywhere on the line the pointer is in the zone of the nearest point. The stylesheet shows
/// the dot of the zone under the pointer, and the title of the zone is the text of that point.
/// </summary>
/// <remarks>
/// Drawn after the parts of the chart, so the zones lie over the markers and the pointer finds them on a
/// dot too. No script follows the pointer: the zones hold without interactivity, where the title stays a
/// native hover text. The zones are hidden from assistive technology, for which the chart's data table
/// already lists every value. Written as a render tree so that it stays internal.
/// </remarks>
internal sealed class OmniChartPointHover : ComponentBase
{
    /// <summary>Radius of the dot of the point under the pointer, in drawing units (the drawing is 100 high).</summary>
    internal const double MarkRadius = 2.5;

    /// <summary>The layout of the chart. An object, so every render of the chart redraws the zones.</summary>
    [Parameter, EditorRequired]
    public OmniChartContext Context { get; set; } = default!;

    /// <summary>Draws the group of zones, one per point shown of every line shown.</summary>
    /// <param name="builder">The render tree builder.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var zones = Context.LineSeries.SelectMany(Zones).ToList();
        if (zones.Count == 0)
        {
            return;
        }

        builder.OpenElement(0, "g");
        builder.AddAttribute(1, "class", "omni-chart__point-hover");
        builder.AddAttribute(2, "aria-hidden", "true");
        foreach (var zone in zones)
        {
            builder.OpenElement(3, "g");
            builder.AddAttribute(4, "class", $"omni-chart__point-zone {ChartColor.Class(zone.ColorIndex)}");
            builder.AddAttribute(5, "data-omni-tip", OmniChartContext.Tip(Context, zone.Text));
            if (Context.IsolateLineOnClick)
            {
                builder.AddAttribute(6, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => Context.IsolateSeries(zone.ColorIndex)));
            }

            builder.OpenElement(8, "polyline");
            builder.AddAttribute(9, "class", "omni-chart__point-reach");
            builder.AddAttribute(10, "points", zone.Reach);
            builder.CloseElement();
            builder.OpenElement(11, "circle");
            builder.AddAttribute(12, "class", "omni-chart__point-mark");
            builder.AddAttribute(13, "cx", OmniChartGeometry.Number(zone.X));
            builder.AddAttribute(14, "cy", OmniChartGeometry.Number(zone.Y));
            builder.AddAttribute(15, "r", OmniChartGeometry.Number(MarkRadius));
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement();
    }

    /// <summary>
    /// The zones of a line, over the points the range shown keeps. The first and last points have one
    /// half only; a point alone repeats itself, so its rounded stroke still draws a place to hover.
    /// </summary>
    private List<Zone> Zones(OmniChartContext.ChartSeriesView series)
    {
        var shown = Enumerable.Range(0, series.Data.Count)
            .Where(Context.InRange)
            .Select(index => (Index: index, Place: Context.ProjectCoordinates(series.Data[index])))
            .Where(point => point.Place.Y > OmniChartContext.OutOfRangeY / 2)
            .ToList();
        var zones = new List<Zone>(shown.Count);
        for (var position = 0; position < shown.Count; position++)
        {
            var (index, place) = shown[position];
            var reach = new List<(double X, double Y)>(3);
            if (position > 0)
            {
                reach.Add(Middle(shown[position - 1].Place, place));
            }

            reach.Add(place);
            if (position < shown.Count - 1)
            {
                reach.Add(Middle(place, shown[position + 1].Place));
            }
            else if (reach.Count == 1)
            {
                reach.Add(place);
            }

            zones.Add(new Zone(place.X, place.Y, series.ColorIndex, Points(reach), Text(series, index)));
        }

        return zones;
    }

    private static (double X, double Y) Middle((double X, double Y) first, (double X, double Y) second) =>
        ((first.X + second.X) / 2, (first.Y + second.Y) / 2);

    private static string Points(IEnumerable<(double X, double Y)> points) =>
        string.Join(' ', points.Select(point => $"{OmniChartGeometry.Number(point.X)},{OmniChartGeometry.Number(point.Y)}"));

    /// <summary>
    /// The text the host gave the point, as it is; else its category and value, after the name of the
    /// series, which the zone hides from the line under it.
    /// </summary>
    private string Text(OmniChartContext.ChartSeriesView series, int index)
    {
        var point = series.Data[index];
        return point.Label ?? (string.IsNullOrWhiteSpace(series.Title)
            ? Context.PointTitle(index, point)
            : OmniChartGeometry.Pair(series.Title, Context.PointTitle(index, point)));
    }

    private sealed record Zone(double X, double Y, int ColorIndex, string Reach, string Text);
}
