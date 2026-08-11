using System.Globalization;

namespace OmniEurope.Blazor.Components;

internal static class OmniChartGeometry
{
    public static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    public static string Points(IReadOnlyList<OmniChartPoint> points) =>
        string.Join(' ', points.Select((_, index) =>
        {
            var point = ProjectedPoint(points, index);
            return $"{Number(point.X)},{Number(point.Y)}";
        }));

    public static string AreaPoints(IReadOnlyList<OmniChartPoint> points)
    {
        if (points.Count == 0)
        {
            return string.Empty;
        }

        var top = Points(points);
        var domain = Domains(points);
        var baseline = ProjectY(0, domain.Y);
        var end = ProjectX(points[^1].X, domain.X);
        var start = ProjectX(points[0].X, domain.X);
        return $"{top} {Number(end)},{Number(baseline)} {Number(start)},{Number(baseline)}";
    }

    public static (double X, double Y) ProjectedPoint(IReadOnlyList<OmniChartPoint> points, int index)
    {
        var domain = Domains(points);
        return (ProjectX(points[index].X, domain.X), ProjectY(points[index].Y, domain.Y));
    }

    public static (double X, double Y, double Width, double Height) ColumnRect(IReadOnlyList<OmniChartPoint> points, int index)
    {
        var domain = Domains(points);
        var slot = 80d / Math.Max(1, points.Count);
        var baseline = ProjectY(0, domain.Y);
        var value = ProjectY(points[index].Y, domain.Y);
        return (10 + index * slot, Math.Min(baseline, value), slot * 0.75, Math.Abs(baseline - value));
    }

    public static (double X, double Y, double Width, double Height) BarRect(IReadOnlyList<OmniChartPoint> points, int index)
    {
        var domain = Domains(points);
        var slot = 80d / Math.Max(1, points.Count);
        var baseline = ProjectX(0, domain.Y);
        var value = ProjectX(points[index].Y, domain.Y);
        return (Math.Min(baseline, value), 10 + index * slot, Math.Abs(baseline - value), slot * 0.75);
    }

    public static string Arc(double startAngle, double endAngle, double radius, bool donut)
    {
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

    public static string Gauge(double value, double radius = 40)
    {
        var endAngle = 180 + Math.Clamp(value, 0, 100) * 1.8;
        var start = Polar(180, radius);
        var end = Polar(endAngle, radius);
        var large = endAngle - 180 > 180 ? 1 : 0;
        return $"M {start} A {Number(radius)} {Number(radius)} 0 {large} 1 {end}";
    }

    private static string Polar(double angle, double radius)
    {
        var radians = (angle - 90) * Math.PI / 180;
        return $"{Number(50 + radius * Math.Cos(radians))} {Number(50 + radius * Math.Sin(radians))}";
    }

    private static ((double Minimum, double Maximum) X, (double Minimum, double Maximum) Y) Domains(IReadOnlyList<OmniChartPoint> points)
    {
        if (points.Count == 0)
        {
            return ((0, 1), (0, 1));
        }

        return (Expand((points.Min(point => point.X), points.Max(point => point.X))),
            Expand((Math.Min(0, points.Min(point => point.Y)), Math.Max(0, points.Max(point => point.Y)))));
    }

    private static double ProjectX(double value, (double Minimum, double Maximum) domain) =>
        5 + Math.Clamp((value - domain.Minimum) / (domain.Maximum - domain.Minimum), 0, 1) * 90;

    private static double ProjectY(double value, (double Minimum, double Maximum) domain) =>
        95 - Math.Clamp((value - domain.Minimum) / (domain.Maximum - domain.Minimum), 0, 1) * 90;

    private static (double Minimum, double Maximum) Expand((double Minimum, double Maximum) domain) =>
        domain.Minimum.Equals(domain.Maximum) ? (domain.Minimum - 0.5, domain.Maximum + 0.5) : domain;
}
