namespace OmniEurope.Blazor.Components;

/// <summary>
/// Whether an <see cref="OmniChart"/> keeps a minimum width (<see cref="OmniChart.MinWidth"/>): in a
/// narrower card, a phone, the chart keeps it and scrolls sideways in its card, as a grid does, instead
/// of shrinking until its text cannot be read.
/// </summary>
public enum OmniChartMinWidth
{
    /// <summary>
    /// The default: a minimum width chosen from the width over height of the drawing, about 12.5rem per
    /// unit of it (12.5rem for a square chart, 20rem up to 1.8, 25rem up to 2.5, 37.5rem up to 3.5,
    /// 50rem beyond). A host sets its own length through the <c>--omni-chart-min-width</c> property.
    /// </summary>
    Auto,

    /// <summary>No minimum: the chart follows the width of its card down to any size, as before.</summary>
    None
}
