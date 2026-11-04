namespace OmniEurope.Blazor.Components;

/// <summary>
/// One sampled position of an <see cref="OmniInkStroke"/>, relative to the drawing area so that a stroke
/// does not depend on the size it was drawn at.
/// </summary>
/// <param name="X">The horizontal position, from 0 (left edge) to 1 (right edge) of the drawing area.</param>
/// <param name="Y">The vertical position, from 0 (top edge) to 1 (bottom edge) of the drawing area.</param>
/// <param name="Pressure">
/// The pen pressure from 0 to 1, which thickens or thins the line around the stroke width (a pressure of 0.5
/// draws it as wide as the stroke says). Null when the device gives none (a mouse, a finger): the line then
/// keeps the stroke width.
/// </param>
public readonly record struct OmniInkPoint(double X, double Y, double? Pressure);
