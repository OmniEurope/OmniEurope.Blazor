namespace OmniEurope.Blazor.Components;

/// <summary>Where an <see cref="OmniLegend"/> sits relative to the plot of its chart.</summary>
public enum OmniLegendPosition
{
    /// <summary>
    /// Right of the plot while the longest entry fits a column of at most a fifth of the drawing,
    /// below it otherwise, so long series names never squeeze the plot.
    /// </summary>
    Auto,

    /// <summary>In a column right of the plot, widened to the longest entry up to 40 % of the drawing.</summary>
    Right,

    /// <summary>
    /// Below the chart, as an HTML list that wraps on narrow screens and keeps the page's text size;
    /// the plot takes the full width.
    /// </summary>
    Bottom
}
