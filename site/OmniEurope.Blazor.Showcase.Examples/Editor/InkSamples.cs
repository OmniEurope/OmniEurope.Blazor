using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>Strokes for the drawing pad demonstration: a wave, a pen underline whose pressure rises, and a dot.</summary>
public static class InkSamples
{
    /// <summary>
    /// A wave in the theme colour, a line under it in blue drawn with a pen pressure that rises from light to firm,
    /// and a dot in red. Positions are relative to the drawing area, like any stroke.
    /// </summary>
    public static IReadOnlyList<OmniInkStroke> Flourish { get; } =
    [
        new(OmniInkStroke.ThemeColor, 3, Enumerable.Range(0, 41)
            .Select(step => new OmniInkPoint(0.08 + (step * 0.0225), 0.4 + (0.22 * Math.Sin(step * Math.PI / 5)), null))
            .ToArray()),
        new("#1a73e8", 4, Enumerable.Range(0, 21)
            .Select(step => new OmniInkPoint(0.08 + (step * 0.045), 0.82, 0.15 + (step * 0.04)))
            .ToArray()),
        new("#d93025", 6, [new OmniInkPoint(0.92, 0.18, null)])
    ];
}
