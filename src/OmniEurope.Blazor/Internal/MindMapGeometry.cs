namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Geometry shared by the rendered markup and by <c>omni-mindmap.js</c>, which redraws the same
/// shapes while a node is dragged. Both sides must produce the same path for the same ends, so the
/// formulas live here once and the script mirrors them.
/// </summary>
internal static class MindMapGeometry
{
    /// <summary>Horizontal padding between the label and the box edge.</summary>
    public const double PaddingX = 22;

    /// <summary>Vertical padding between the label and the box edge.</summary>
    public const double PaddingY = 14;

    /// <summary>The narrowest box an automatically sized node gets.</summary>
    public const double MinimumAutoWidth = 80;

    /// <summary>Numbers written into SVG attributes: invariant culture, at most three decimals.</summary>
    public static string Format(double value) =>
        Math.Round(value, 3).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// The curve of a link: a cubic Bezier leaving and reaching each node horizontally, its control
    /// points at 40 and 60 percent of the horizontal distance.
    /// </summary>
    public static string EdgePath(double fromX, double fromY, double toX, double toY)
    {
        var dx = toX - fromX;
        return $"M {Format(fromX)} {Format(fromY)} C {Format(fromX + (dx * 0.4))} {Format(fromY)}, {Format(fromX + (dx * 0.6))} {Format(toY)}, {Format(toX)} {Format(toY)}";
    }

    /// <summary>The <c>transform</c> that places a node group on its centre.</summary>
    public static string Translate(double x, double y) => $"translate({Format(x)} {Format(y)})";

    /// <summary>The <c>transform</c> of the viewport group for a pan and zoom.</summary>
    public static string View(double panX, double panY, double zoom) =>
        $"translate({Format(panX)} {Format(panY)}) scale({Format(zoom)})";

    /// <summary>
    /// The size of the text of a label the browser has not measured yet: an average glyph width of
    /// 0.55 em, 0.6 em when bold, and a line of 1.2 em. The measured size replaces it after render.
    /// </summary>
    public static (double Width, double Height) EstimateText(string label, int fontSize, bool bold) =>
        (label.Length * fontSize * (bold ? 0.6 : 0.55), fontSize * 1.2);
}
