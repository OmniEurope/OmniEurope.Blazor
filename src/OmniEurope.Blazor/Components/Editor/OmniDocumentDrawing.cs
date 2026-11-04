namespace OmniEurope.Blazor.Components;

/// <summary>A drawing attached to a paragraph: freehand strokes over a sheet of a given size.</summary>
/// <param name="Strokes">The strokes (see <see cref="OmniInkStroke"/>), relative to the sheet, so they keep their look at any size.</param>
/// <param name="WidthPt">The width of the sheet in points (1/72 inch), above zero.</param>
/// <param name="HeightPt">The height of the sheet in points, above zero. The proportions of the sheet are those of the drawing area.</param>
public sealed record OmniDocumentDrawing(IReadOnlyList<OmniInkStroke> Strokes, double WidthPt, double HeightPt);
