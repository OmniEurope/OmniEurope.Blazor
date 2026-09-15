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

    /// <summary>
    /// The box of a node before the browser has measured its label: its fixed size when it has one,
    /// otherwise the estimated label plus padding, never narrower than <see cref="MinimumAutoWidth"/>.
    /// </summary>
    public static (double Width, double Height) BoxOf(Components.OmniMindMapNode node)
    {
        var fontSize = node.FontSize > 0 ? node.FontSize : Components.OmniMindMapNode.DefaultFontSize;
        var (textWidth, textHeight) = EstimateText(node.Label, fontSize, node.Bold);
        return (
            node.Width > 0 ? node.Width : Math.Max(textWidth + (PaddingX * 2), MinimumAutoWidth),
            node.Height > 0 ? node.Height : textHeight + (PaddingY * 2));
    }

    /// <summary>
    /// The curve of a directed link: it leaves the border of the first box and stops on the border of
    /// the second, where the arrowhead sits, so the head is never hidden under the box it points to.
    /// It runs horizontally when the boxes are further apart across than down, vertically otherwise,
    /// with its control points halfway along that axis. <c>omni-mindmap.js</c> mirrors it.
    /// </summary>
    public static string DirectedEdgePath(
        double fromX, double fromY, double fromWidth, double fromHeight,
        double toX, double toY, double toWidth, double toHeight)
    {
        var dx = toX - fromX;
        var dy = toY - fromY;
        var gapAcross = Math.Abs(dx) - ((fromWidth + toWidth) / 2);
        var gapDown = Math.Abs(dy) - ((fromHeight + toHeight) / 2);
        if (gapAcross >= gapDown)
        {
            var sign = dx >= 0 ? 1 : -1;
            var startX = fromX + (sign * fromWidth / 2);
            var endX = toX - (sign * toWidth / 2);
            var bend = (endX - startX) / 2;
            return $"M {Format(startX)} {Format(fromY)} C {Format(startX + bend)} {Format(fromY)}, {Format(endX - bend)} {Format(toY)}, {Format(endX)} {Format(toY)}";
        }

        var down = dy >= 0 ? 1 : -1;
        var startY = fromY + (down * fromHeight / 2);
        var endY = toY - (down * toHeight / 2);
        var curve = (endY - startY) / 2;
        return $"M {Format(fromX)} {Format(startY)} C {Format(fromX)} {Format(startY + curve)}, {Format(toX)} {Format(endY - curve)}, {Format(toX)} {Format(endY)}";
    }
}
