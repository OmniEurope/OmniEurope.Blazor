namespace OmniEurope.Blazor.Components;

/// <summary>
/// The shared tooltip of an <see cref="OmniChart"/> with <see cref="OmniChart.SharedTooltip"/>: one
/// transparent band per category, across the whole plot, whose SVG title names the category and then
/// the value of every series at that category, one line per series in the order they registered.
/// </summary>
/// <remarks>
/// Drawn after the parts of the chart, so the bands lie over the series and the pointer finds them
/// anywhere in the plot; for the same reason the bands render after the value axis has handed its
/// <c>FormatValue</c> to the layout. Categories are counted by index, like the category axis and the
/// data table: band <c>i</c> covers the points of index <c>i</c> of every series. The bands are
/// hidden from assistive technology, for which the chart's data table already lists every value.
/// Written as a render tree so that it stays internal.
/// </remarks>
internal sealed class OmniChartSharedTooltip : ComponentBase
{
    /// <summary>The layout of the chart. An object, so every render of the chart redraws the bands.</summary>
    [Parameter, EditorRequired]
    public OmniChartContext Context { get; set; } = default!;

    /// <summary>The name of a series in the tooltip: its title, else the chart's "Series 2" fallback.</summary>
    [Parameter, EditorRequired]
    public Func<OmniChartContext.ChartSeriesView, int, string> SeriesName { get; set; } = default!;

    /// <summary>Draws the group of bands, one per category that holds at least one value.</summary>
    /// <param name="builder">The render tree builder.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var bands = Bands();
        if (bands.Count == 0)
        {
            return;
        }

        builder.OpenElement(0, "g");
        builder.AddAttribute(1, "class", "omni-chart__hover");
        builder.AddAttribute(2, "aria-hidden", "true");
        foreach (var band in bands)
        {
            builder.OpenElement(3, "rect");
            builder.AddAttribute(4, "class", "omni-chart__hover-band");
            builder.AddAttribute(5, "x", OmniChartGeometry.Number(band.X));
            builder.AddAttribute(6, "y", OmniChartGeometry.Number(band.Y));
            builder.AddAttribute(7, "width", OmniChartGeometry.Number(band.Width));
            builder.AddAttribute(8, "height", OmniChartGeometry.Number(band.Height));
            builder.OpenElement(9, "title");
            builder.AddContent(10, band.Text);
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement();
    }

    /// <summary>
    /// The band of each category: from halfway to the previous category to halfway to the next one
    /// (the plot's edge for the first and the last), which is exactly the category band when the
    /// chart has columns or bars. Across the plot on a vertical chart, down it on a horizontal one.
    /// </summary>
    private List<(double X, double Y, double Width, double Height, string Text)> Bands()
    {
        var series = Context.TableSeries;
        var count = Math.Max(Context.CategoryLabelCount, series.Select(item => item.Data.Count).DefaultIfEmpty(0).Max());
        var bands = new List<(double, double, double, double, string)>(count);
        if (series.Count == 0)
        {
            return bands;
        }

        var horizontal = Context.Horizontal;
        var (start, end) = horizontal
            ? (OmniChartContext.PlotTop, OmniChartContext.PlotBottom)
            : (Context.PlotLeft, Context.PlotRight);
        for (var index = 0; index < count; index++)
        {
            if (Text(series, index) is not { } text)
            {
                continue;
            }

            var from = index == 0 ? start : Middle(index - 1, index, count);
            var to = index == count - 1 ? end : Middle(index, index + 1, count);
            bands.Add(horizontal
                ? (Context.PlotLeft, from, Context.PlotRight - Context.PlotLeft, to - from, text)
                : (from, OmniChartContext.PlotTop, to - from, OmniChartContext.PlotBottom - OmniChartContext.PlotTop, text));
        }

        return bands;
    }

    private double Middle(int first, int second, int count) =>
        (Context.CategoryPosition(first, count) + Context.CategoryPosition(second, count)) / 2;

    /// <summary>
    /// The category on the first line (its axis label, else the X value of the first series that has
    /// the point), then one line per series that has a point there: its name and its value, written
    /// by the value axis's format. Null when no series has a point at that index.
    /// </summary>
    private string? Text(IReadOnlyList<OmniChartContext.ChartSeriesView> series, int index)
    {
        var lines = new List<string>(series.Count + 1);
        for (var column = 0; column < series.Count; column++)
        {
            if (index < series[column].Data.Count)
            {
                lines.Add(OmniChartGeometry.Pair(SeriesName(series[column], column), Context.FormatValue(series[column].Data[index].Y)));
            }
        }

        if (lines.Count == 0)
        {
            return null;
        }

        var category = Context.CategoryLabel(index)
            ?? OmniChartGeometry.Display(series.First(item => index < item.Data.Count).Data[index].X);
        lines.Insert(0, category);
        return string.Join('\n', lines);
    }
}
