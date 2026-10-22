using System.Globalization;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Chart arithmetic and number writing. Two ways to write a number: <see cref="Number"/> for SVG
/// geometry (invariant, the only form an attribute accepts) and <see cref="Display"/> for text a
/// reader sees or hears (the current culture: a comma decimal separator in French).
/// </summary>
internal static class OmniChartGeometry
{
    /// <summary>Between a category and its value in a hover text, whatever the chart: "mars · 12".</summary>
    public const string Separator = " · ";

    /// <summary>A number for an SVG attribute: invariant culture, at most three decimals; "0" for a value that is not finite, which no attribute accepts.</summary>
    public static string Number(double value) => double.IsFinite(value) ? value.ToString("0.###", CultureInfo.InvariantCulture) : "0";

    /// <summary>A number for display: current culture, at most three decimals.</summary>
    public static string Display(double value) => value.ToString("0.###", CultureInfo.CurrentCulture);

    /// <summary>A category and its value, joined by <see cref="Separator"/>.</summary>
    public static string Pair(string category, string value) => category + Separator + value;

    /// <summary>
    /// The points of a polyline. A point whose X or Y is not a finite number is left out: it has no place
    /// on the drawing, and a "NaN" in an SVG attribute would invalidate the whole shape.
    /// </summary>
    public static string Points(IReadOnlyList<OmniChartPoint> points) => Points(points, Domains(points));

    internal static string Points(IReadOnlyList<OmniChartPoint> points, ChartDomain domain)
    {
        var text = new System.Text.StringBuilder();
        for (var index = 0; index < points.Count; index++)
        {
            var point = points[index];
            if (!IsFinite(point))
            {
                continue;
            }

            if (text.Length > 0)
            {
                text.Append(' ');
            }

            text.Append(Number(ProjectX(point.X, domain.X))).Append(',').Append(Number(ProjectY(point.Y, domain.Y)));
        }

        return text.ToString();
    }

    public static string AreaPoints(IReadOnlyList<OmniChartPoint> points) => AreaPoints(points, Domains(points));

    internal static string AreaPoints(IReadOnlyList<OmniChartPoint> points, ChartDomain domain)
    {
        var top = Points(points, domain);
        if (top.Length == 0)
        {
            return string.Empty;
        }

        var first = 0;
        while (!IsFinite(points[first]))
        {
            first++;
        }

        var last = points.Count - 1;
        while (!IsFinite(points[last]))
        {
            last--;
        }

        var baseline = ProjectY(0, domain.Y);
        var end = ProjectX(points[last].X, domain.X);
        var start = ProjectX(points[first].X, domain.X);
        return $"{top} {Number(end)},{Number(baseline)} {Number(start)},{Number(baseline)}";
    }

    public static (double X, double Y) ProjectedPoint(IReadOnlyList<OmniChartPoint> points, int index) =>
        ProjectedPoint(points, index, Domains(points));

    internal static (double X, double Y) ProjectedPoint(IReadOnlyList<OmniChartPoint> points, int index, ChartDomain domain) =>
        (ProjectX(points[index].X, domain.X), ProjectY(points[index].Y, domain.Y));

    public static (double X, double Y, double Width, double Height) ColumnRect(IReadOnlyList<OmniChartPoint> points, int index) =>
        ColumnRect(points, index, Domains(points));

    internal static (double X, double Y, double Width, double Height) ColumnRect(IReadOnlyList<OmniChartPoint> points, int index, ChartDomain domain)
    {
        var slot = 80d / Math.Max(1, points.Count);
        var baseline = ProjectY(0, domain.Y);
        var value = ProjectY(points[index].Y, domain.Y);
        return (10 + index * slot, Math.Min(baseline, value), slot * 0.75, Math.Abs(baseline - value));
    }

    public static (double X, double Y, double Width, double Height) BarRect(IReadOnlyList<OmniChartPoint> points, int index) =>
        BarRect(points, index, Domains(points));

    internal static (double X, double Y, double Width, double Height) BarRect(IReadOnlyList<OmniChartPoint> points, int index, ChartDomain domain)
    {
        var slot = 80d / Math.Max(1, points.Count);
        var baseline = ProjectX(0, domain.Y);
        var value = ProjectX(points[index].Y, domain.Y);
        return (Math.Min(baseline, value), 10 + index * slot, Math.Abs(baseline - value), slot * 0.75);
    }

    public static string Arc(double startAngle, double endAngle, double radius, bool donut)
    {
        if (endAngle - startAngle >= 359.99)
        {
            return FullRing(radius, donut ? radius * 0.58 : 0);
        }

        var start = Polar(startAngle, radius);
        var end = Polar(endAngle, radius);
        var large = endAngle - startAngle > 180 ? 1 : 0;
        if (!donut)
        {
            return $"M 50 50 L {start} A {Number(radius)} {Number(radius)} 0 {large} 1 {end} Z";
        }

        var inner = radius * 0.58;
        var innerEnd = Polar(endAngle, inner);
        var innerStart = Polar(startAngle, inner);
        return $"M {start} A {Number(radius)} {Number(radius)} 0 {large} 1 {end} L {innerEnd} A {Number(inner)} {Number(inner)} 0 {large} 0 {innerStart} Z";
    }

    /// <summary>
    /// The arc of a gauge filled to <paramref name="value"/> percent: a half circle that starts at its
    /// left end and runs over the top, clockwise, towards its right end.
    /// </summary>
    public static string Gauge(double value, double radius = 40)
    {
        const double LeftEnd = 270;
        var endAngle = LeftEnd + (Math.Clamp(value, 0, 100) * 1.8);
        var start = Polar(LeftEnd, radius);
        var end = Polar(endAngle, radius);
        return $"M {start} A {Number(radius)} {Number(radius)} 0 0 1 {end}";
    }

    /// <summary>
    /// A whole disc, or a whole ring when <paramref name="inner"/> is positive, drawn as two half
    /// arcs: a single arc cannot start and end on the same point, and 359.999 degrees left a notch.
    /// </summary>
    private static string FullRing(double radius, double inner)
    {
        var outer = $"M 50 {Number(50 - radius)} A {Number(radius)} {Number(radius)} 0 1 1 50 {Number(50 + radius)} A {Number(radius)} {Number(radius)} 0 1 1 50 {Number(50 - radius)} Z";
        if (inner <= 0)
        {
            return outer;
        }

        // The inner circle runs the other way round, so the non-zero fill rule leaves it empty.
        return $"{outer} M 50 {Number(50 - inner)} A {Number(inner)} {Number(inner)} 0 1 0 50 {Number(50 + inner)} A {Number(inner)} {Number(inner)} 0 1 0 50 {Number(50 - inner)} Z";
    }

    private static string Polar(double angle, double radius)
    {
        var radians = (angle - 90) * Math.PI / 180;
        return $"{Number(50 + radius * Math.Cos(radians))} {Number(50 + radius * Math.Sin(radians))}";
    }

    /// <summary>The ranges of X and Y a series is drawn over, from the finite points only, in one pass.</summary>
    internal readonly record struct ChartDomain((double Minimum, double Maximum) X, (double Minimum, double Maximum) Y);

    internal static ChartDomain Domains(IReadOnlyList<OmniChartPoint> points)
    {
        double minimumX = double.PositiveInfinity, maximumX = double.NegativeInfinity;
        double minimumY = 0, maximumY = 0;
        var any = false;
        for (var index = 0; index < points.Count; index++)
        {
            var point = points[index];
            if (!IsFinite(point))
            {
                continue;
            }

            any = true;
            minimumX = Math.Min(minimumX, point.X);
            maximumX = Math.Max(maximumX, point.X);
            minimumY = Math.Min(minimumY, point.Y);
            maximumY = Math.Max(maximumY, point.Y);
        }

        return any
            ? new ChartDomain(Expand((minimumX, maximumX)), Expand((minimumY, maximumY)))
            : new ChartDomain((0, 1), (0, 1));
    }

    private static bool IsFinite(OmniChartPoint point) => double.IsFinite(point.X) && double.IsFinite(point.Y);

    // A value that is not a number sits at the start of the range (X) or on the baseline (Y): never a NaN.
    private static double ProjectX(double value, (double Minimum, double Maximum) domain) =>
        5 + Math.Clamp(Fraction(value, domain), 0, 1) * 90;

    private static double ProjectY(double value, (double Minimum, double Maximum) domain) =>
        95 - Math.Clamp(Fraction(double.IsNaN(value) ? 0 : value, domain), 0, 1) * 90;

    private static double Fraction(double value, (double Minimum, double Maximum) domain) =>
        double.IsNaN(value) ? 0 : (value - domain.Minimum) / (domain.Maximum - domain.Minimum);

    private static (double Minimum, double Maximum) Expand((double Minimum, double Maximum) domain) =>
        domain.Minimum.Equals(domain.Maximum) ? (domain.Minimum - 0.5, domain.Maximum + 0.5) : domain;
}
