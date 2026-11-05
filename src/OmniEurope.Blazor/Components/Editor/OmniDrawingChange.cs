namespace OmniEurope.Blazor.Components;

/// <summary>A drawing the reader drew for a paragraph, as strokes and as a picture.</summary>
/// <param name="BlockId">The <see cref="OmniDocumentBlock.Id"/> of the paragraph the drawing belongs to.</param>
/// <param name="Drawing">The drawing: its strokes and the size of its sheet.</param>
/// <param name="Png">
/// The drawing as a PNG with a transparent background, <see cref="OmniDocumentDrawing.WidthPt"/> by
/// <see cref="OmniDocumentDrawing.HeightPt"/> converted to pixels at 96 dpi (points times 96 over 72, rounded), made by
/// <see cref="OmniInkCanvas.ExportPngAsync(int, int, CancellationToken)"/>; a stroke without a colour is black in it.
/// </param>
public sealed record OmniDrawingChange(string BlockId, OmniDocumentDrawing Drawing, byte[] Png);
