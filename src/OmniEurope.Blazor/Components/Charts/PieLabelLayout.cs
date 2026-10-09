namespace OmniEurope.Blazor.Components;

/// <summary>
/// The outside labels of a pie: the disc shrinks to leave room for the longest label on each side, every
/// label sits on its side of the disc, joined to the middle of its slice by a leader line, and the labels
/// of one side are stacked so that none overlaps the next. All in chart units (the drawing is 100 high,
/// centred on 50, 50).
/// </summary>
internal static class PieLabelLayout
{
    /// <summary>The radius of a pie drawn without outside labels.</summary>
    internal const double FullRadius = 42;

    // Smallest disc kept beside long labels; a label that still does not fit is shortened.
    private const double MinimumRadius = 16;
    private const double Elbow = 4;
    private const double Run = 4;
    private const double TextGap = 1;
    private const double LineHeight = OmniChartContext.FontSize * 1.25;
    private const double Top = 3;
    private const double Bottom = 99;

    /// <summary>One label: its leader line (three points), where its text starts, its anchor and its text.</summary>
    internal sealed record Label(string Points, double X, double Y, string? Anchor, string Text, string FullText);

    /// <summary>The radius that leaves room on each side for the longest of <paramref name="texts"/>.</summary>
    /// <param name="texts">The labels.</param>
    /// <param name="halfWidth">Half the width of the drawing, 50 plus the spread of a wide chart.</param>
    internal static double Radius(IReadOnlyList<string> texts, double halfWidth)
    {
        var longest = texts.Count == 0 ? 0 : texts.Max(text => text.Length) * OmniChartContext.CharacterWidth;
        return Math.Clamp(halfWidth - Elbow - Run - TextGap - longest - 1, MinimumRadius, FullRadius);
    }

    /// <summary>Lays out one label per slice, its middle angle in <paramref name="middles"/> (degrees, clockwise from the top).</summary>
    internal static IReadOnlyList<Label> Place(IReadOnlyList<string> texts, IReadOnlyList<double> middles, double radius, double halfWidth)
    {
        var labels = new Label?[texts.Count];
        foreach (var right in new[] { true, false })
        {
            // The slices of one side, from top to bottom, each wanting the height of its middle.
            var side = Enumerable.Range(0, texts.Count)
                .Where(index => (Math.Sin(Radians(middles[index])) >= 0) == right)
                .Select(index => (Index: index, Wanted: 50 - ((radius + Elbow) * Math.Cos(Radians(middles[index])))))
                .OrderBy(item => item.Wanted)
                .ToList();
            var heights = Stack([.. side.Select(item => item.Wanted)]);
            var room = Math.Max(OmniChartContext.CharacterWidth, halfWidth - radius - Elbow - Run - TextGap - 1);
            for (var position = 0; position < side.Count; position++)
            {
                var index = side[position].Index;
                var angle = Radians(middles[index]);
                var (sin, cos) = (Math.Sin(angle), Math.Cos(angle));
                var y = heights[position];
                var edge = (X: 50 + (radius * sin), Y: 50 - (radius * cos));
                var elbow = (X: 50 + ((radius + Elbow) * sin), Y: y);
                var end = (X: right ? 50 + radius + Elbow + Run : 50 - radius - Elbow - Run, Y: y);
                var points = string.Join(' ', new[] { edge, elbow, end }.Select(point => $"{OmniChartGeometry.Number(point.X)},{OmniChartGeometry.Number(point.Y)}"));
                var text = Fit(texts[index], room);
                labels[index] = new Label(points, right ? end.X + TextGap : end.X - TextGap, y, right ? null : "end", text, texts[index]);
            }
        }

        return [.. labels.Select(label => label!)];
    }

    /// <summary>Pushes each height down to one line under the previous one, then the whole side back up if it runs out at the bottom.</summary>
    internal static double[] Stack(double[] wanted)
    {
        var heights = new double[wanted.Length];
        for (var index = 0; index < wanted.Length; index++)
        {
            heights[index] = Math.Max(wanted[index], index == 0 ? Top : heights[index - 1] + LineHeight);
        }

        for (var index = wanted.Length - 1; index >= 0; index--)
        {
            var limit = index == wanted.Length - 1 ? Bottom : heights[index + 1] - LineHeight;
            heights[index] = Math.Max(Top, Math.Min(heights[index], limit));
        }

        return heights;
    }

    // A label wider than its room is cut with an ellipsis; its hover text keeps it whole.
    private static string Fit(string text, double room)
    {
        var characters = Math.Max(1, (int)Math.Floor(room / OmniChartContext.CharacterWidth));
        return text.Length <= characters ? text : string.Concat(text.AsSpan(0, Math.Max(0, characters - 1)), "…");
    }

    private static double Radians(double degrees) => degrees * Math.PI / 180;
}
