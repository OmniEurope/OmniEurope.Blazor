using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>The value of each point written above it, or past the end of a horizontal bar.</summary>
/// <remarks>
/// A chart part: it derives from <see cref="ComponentBase"/>, not <see cref="OmniComponentBase"/>, on
/// purpose. It draws SVG inside its chart, so it takes no <c>Id</c>, <c>Class</c> or extra attributes.
/// </remarks>
public partial class OmniSeriesDataLabels
{
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }

    /// <summary>The points to label, the same as the series they decorate.</summary>
    [Parameter] public IReadOnlyList<OmniChartPoint> Data { get; set; } = Array.Empty<OmniChartPoint>();

    /// <summary>
    /// Writes a value as text; by default the number in the current culture, at most three decimals. A
    /// point with its own <see cref="OmniChartPoint.Label"/> shows that label instead.
    /// </summary>
    [Parameter] public Func<double, string>? FormatValue { get; set; }

    /// <summary>
    /// Writes each label inside its bar, from the start of a horizontal bar or under the top of a column,
    /// in the colour <see cref="InsideContrast"/> gives it. Off by default: above the point, or past the
    /// end of a horizontal bar.
    /// </summary>
    [Parameter] public bool Inside { get; set; }

    /// <summary>
    /// With <see cref="Inside"/>, writes a label in black or white, whichever reads best on the colour of
    /// its bar in the current theme, and a label its bar cannot hold outside the bar, where it would have
    /// been without <see cref="Inside"/>. On by default; <c>false</c> keeps every label inside, in the
    /// text colour outlined with the surface colour, a long one running on past the end of its bar.
    /// </summary>
    [Parameter] public bool InsideContrast { get; set; } = true;

    private string LabelsClass => Inside ? "omni-chart__labels omni-chart__labels--inside" : "omni-chart__labels";

    /// <summary>Registers the points with the chart so the plot covers them; data labels stay out of the legend and the data table.</summary>
    protected override void OnParametersSet()
    {
        _domain = null;
        ChartContext?.RegisterSeries(this, OmniChartSeriesKind.Auxiliary, Data);
    }

    // Without a chart around the markers, the range of the points is read once per render, not once per dot.
    private OmniChartGeometry.ChartDomain? _domain;

    private OmniChartGeometry.ChartDomain Domain => _domain ??= OmniChartGeometry.Domains(Data);

    /// <summary>
    /// Every label with its place, anchor and baseline, and whether it is drawn: a label that would
    /// overlap the previous one kept is masked, the rule of the category axis, the last one always kept
    /// (see <see cref="DataLabelThinning"/>). A point out of the range shown takes no part.
    /// </summary>
    internal IReadOnlyList<DataLabel> Layout()
    {
        var labels = new DataLabel[Data.Count];
        var boxes = new DataLabelThinning.Box?[Data.Count];
        // Only a chart lays out horizontal bars, so a horizontal layout always has its chart at hand.
        var horizontalChart = ChartContext is { Horizontal: true } chart ? chart : null;
        var horizontal = horizontalChart is not null;
        // The bars this series of labels is written on, when it follows them.
        var bars = Inside && InsideContrast ? ChartContext?.BarsOf(Data) : null;
        for (var index = 0; index < Data.Count; index++)
        {
            var projected = Projected(index);
            var text = Text(Data[index]);
            var bar = bars?[index];
            var inside = Inside && (bar is not { } held || Holds(held, text, horizontal));
            var (x, anchor, y, baseline) = Position(Data[index], projected, text, inside, horizontalChart);
            var barClass = inside && bar is { } colored ? $"omni-chart__label-on-bar {ChartColor.Class(colored.ColorIndex)}" : null;
            labels[index] = new DataLabel(x, y, anchor, baseline, text, true, projected, barClass);
            if (projected.Y > OmniChartContext.OutOfRangeY / 2)
            {
                boxes[index] = DataLabelThinning.BoxOf(x, y, anchor, baseline, text.Length * OmniChartContext.CharacterWidth, OmniChartContext.FontSize);
            }
        }

        var visible = DataLabelThinning.Keep(boxes);
        for (var index = 0; index < labels.Length; index++)
        {
            if (!visible[index])
            {
                labels[index] = labels[index] with { Visible = false };
            }
        }

        return labels;
    }

    /// <summary>Where a label is written and how: inside its bar or past it, along a horizontal bar or over a point.</summary>
    private (double X, string? Anchor, double Y, string? Baseline) Position(
        OmniChartPoint point, (double X, double Y) projected, string text, bool inside, OmniChartContext? horizontalChart)
    {
        if (inside && horizontalChart is not null)
        {
            // Where the value axis starts (zero): a label inside a horizontal bar begins there.
            var (x, anchor) = Placed(horizontalChart.ProjectCoordinates(point with { Y = 0 }).X + 1.5, text, LabelAnchor.Start);
            return (x, anchor, projected.Y, "central");
        }

        if (horizontalChart is not null)
        {
            var (x, anchor) = Placed(projected.X + 1.5, text, LabelAnchor.Start);
            return (x, anchor, projected.Y, "central");
        }

        var (middle, middleAnchor) = Placed(projected.X, text, LabelAnchor.Middle);
        return inside ? (middle, middleAnchor, projected.Y + 1.5, "hanging") : (middle, middleAnchor, projected.Y - 3, null);
    }

    /// <summary>One label: where it is written, how, and whether it is drawn or masked.</summary>
    internal sealed record DataLabel(double X, double Y, string? Anchor, string? Baseline, string Text, bool Visible, (double X, double Y) Point, string? BarClass = null);

    /// <summary>
    /// Whether a bar holds a label: its estimated width and the margins along a horizontal bar, across a
    /// column, and the height of the text with, under the top of a column, its margin.
    /// </summary>
    private static bool Holds(OmniChartContext.ChartBar bar, string text, bool horizontal)
    {
        var width = text.Length * OmniChartContext.CharacterWidth;
        return horizontal
            ? width + (2 * InsideMargin) <= bar.Width && OmniChartContext.FontSize <= bar.Height
            : width <= bar.Width && OmniChartContext.FontSize + (2 * InsideMargin) <= bar.Height;
    }

    private string Text(OmniChartPoint point) => point.Label ?? FormatValue?.Invoke(point.Y) ?? OmniChartGeometry.Display(point.Y);

    /// <summary>
    /// Where a label is drawn and how it is anchored: as asked, unless its estimated width
    /// (<see cref="OmniChartContext.CharacterWidth"/> per character, an estimate that errs towards more
    /// room) would cross an edge of the plot; then it is anchored to that edge, start on the left and end
    /// on the right, so the first and last labels stay inside instead of overflowing by half their width.
    /// </summary>
    internal (double X, string? Anchor) Placed(double x, string text, LabelAnchor anchor)
    {
        var asked = anchor == LabelAnchor.Middle ? "middle" : null;
        if (ChartContext is null)
        {
            return (x, asked);
        }

        var width = text.Length * OmniChartContext.CharacterWidth;
        var (left, right) = anchor == LabelAnchor.Middle ? (x - (width / 2), x + (width / 2)) : (x, x + width);
        if (left < ChartContext.PlotLeft)
        {
            return (ChartContext.PlotLeft, null);
        }

        return right > ChartContext.PlotRight ? (ChartContext.PlotRight, "end") : (x, asked);
    }

    /// <summary>How a label is anchored when it fits: centred on its point, or starting at it.</summary>
    internal enum LabelAnchor
    {
        Middle,
        Start
    }

    private (double X, double Y) Projected(int index) =>
        ChartContext is null ? OmniChartGeometry.ProjectedPoint(Data, index, Domain) : ChartContext.ProjectCoordinates(Data[index]);

    // The room left between a label inside its bar and the end of the bar.
    private const double InsideMargin = 1.5;

    /// <summary>Removes the labels from their chart.</summary>
    public void Dispose()
    {
        ChartContext?.UnregisterSeries(this);
        GC.SuppressFinalize(this);
    }

    /// <summary>Decorates a series hidden through a legend entry: it draws nothing either.</summary>
    private bool Hidden => ChartContext?.IsDataHidden(Data) == true;
}
