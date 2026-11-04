using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The flat form in which strokes cross to and from <c>omni-ink-canvas.js</c>: a colour and a width per stroke,
/// and the points of a stroke as one array of numbers (<c>x, y, pressure</c> per point, a pressure of -1 for
/// "none"). Plain arrays of numbers keep the crossing independent of how a library type would be serialized.
/// </summary>
internal static class InkStrokeCodec
{
    private const int Stride = 3;
    private const int MaxPoints = 20_000;
    private const int MaxColorLength = 64;
    private const double NoPressure = -1d;

    /// <summary>The narrowest and the widest line a stroke can have, in CSS pixels at the reference width.</summary>
    internal const double MinWidth = 0.25d;

    /// <inheritdoc cref="MinWidth"/>
    internal const double MaxWidth = 48d;

    /// <summary>The three parallel arrays the script reads: one entry per stroke.</summary>
    internal static (string[] Colors, double[] Widths, double[][] Points) Pack(IReadOnlyList<OmniInkStroke> strokes)
    {
        var colors = new string[strokes.Count];
        var widths = new double[strokes.Count];
        var points = new double[strokes.Count][];
        for (var index = 0; index < strokes.Count; index++)
        {
            var stroke = strokes[index];
            colors[index] = stroke.Color;
            widths[index] = stroke.Width;
            var flat = new double[stroke.Points.Count * Stride];
            for (var at = 0; at < stroke.Points.Count; at++)
            {
                var point = stroke.Points[at];
                flat[at * Stride] = point.X;
                flat[(at * Stride) + 1] = point.Y;
                flat[(at * Stride) + 2] = point.Pressure ?? NoPressure;
            }

            points[index] = flat;
        }

        return (colors, widths, points);
    }

    /// <summary>
    /// Reads a stroke the script reports. A value that cannot be a stroke the script would draw (no point,
    /// a length that is no whole number of points, a point that is not a number, too many points) is refused;
    /// coordinates are brought back into the drawing area, the width into the allowed range, and a colour
    /// that is empty or too long becomes the theme colour.
    /// </summary>
    internal static bool TryUnpack(string? color, double width, double[]? flat, out OmniInkStroke stroke)
    {
        stroke = null!;
        if (flat is null || flat.Length == 0 || flat.Length % Stride != 0 || flat.Length > MaxPoints * Stride
            || !double.IsFinite(width) || flat.Any(value => !double.IsFinite(value)))
        {
            return false;
        }

        var points = new OmniInkPoint[flat.Length / Stride];
        for (var at = 0; at < points.Length; at++)
        {
            var pressure = flat[(at * Stride) + 2];
            points[at] = new OmniInkPoint(
                Math.Clamp(flat[at * Stride], 0d, 1d),
                Math.Clamp(flat[(at * Stride) + 1], 0d, 1d),
                pressure < 0d ? null : Math.Min(pressure, 1d));
        }

        var usable = !string.IsNullOrWhiteSpace(color) && color.Length <= MaxColorLength;
        stroke = new OmniInkStroke(usable ? color! : OmniInkStroke.ThemeColor, Math.Clamp(width, MinWidth, MaxWidth), points);
        return true;
    }
}
