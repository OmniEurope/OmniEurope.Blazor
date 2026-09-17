namespace OmniEurope.Blazor.Components;

/// <summary>The centre of a laid out node.</summary>
/// <param name="X">Horizontal position, from the left edge of the drawing.</param>
/// <param name="Y">Vertical position, from the top edge of the drawing.</param>
public readonly record struct OmniGraphPoint(double X, double Y);
