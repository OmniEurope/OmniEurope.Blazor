using System.Globalization;

namespace OmniEurope.Blazor.Components;

public sealed record OmniChartPoint(double X, double Y, string? Label = null);
public sealed record OmniChartSlice(string Label, double Value);

internal static class OmniChartGeometry
{
    public static double Clamp(double value) => Math.Clamp(value, 0, 100);
    public static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    public static string Xy(double x, double y) => $"{Number(Clamp(x))},{Number(100 - Clamp(y))}";
    public static string Points(IEnumerable<OmniChartPoint> points) => string.Join(' ', points.Select(point => Xy(point.X, point.Y)));

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
        var endAngle = 180 + Clamp(value) * 1.8;
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
}
