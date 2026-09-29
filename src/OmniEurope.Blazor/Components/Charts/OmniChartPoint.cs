namespace OmniEurope.Blazor.Components;

/// <summary>One point of a line, area or column series, or of the markers and data labels over it.</summary>
/// <param name="X">The position along the category axis: points are placed between the lowest and highest X of the chart's series (the index of the category in the usual case).</param>
/// <param name="Y">The value, placed on the value axis.</param>
/// <param name="Label">Text of the point, shown instead of its value in its hover text and data label; null (the default) writes the value (with its category in a chart).</param>
public sealed record OmniChartPoint(double X, double Y, string? Label = null);
