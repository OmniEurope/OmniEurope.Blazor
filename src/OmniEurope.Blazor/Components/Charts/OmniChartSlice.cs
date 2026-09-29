namespace OmniEurope.Blazor.Components;

/// <summary>One slice of an <see cref="OmniPieSeries"/>.</summary>
/// <param name="Label">The name of the slice, shown by its hover text, the legend and the chart's data table.</param>
/// <param name="Value">The share of the slice, drawn in proportion to the sum of the slices; a slice of zero or less is left out.</param>
public sealed record OmniChartSlice(string Label, double Value);
