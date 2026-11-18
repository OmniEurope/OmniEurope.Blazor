namespace OmniEurope.Blazor.Components;

/// <summary>
/// One freehand line of an <see cref="OmniInkCanvas"/>: a colour, a width and the sampled points, put down
/// between a pointer press and its release.
/// </summary>
/// <remarks>
/// <para>
/// The points are relative to the drawing area (see <see cref="OmniInkPoint"/>) and the width is relative to
/// its width, so a drawing keeps its look at any size, like an image: <see cref="Width"/> is the width of the
/// line in CSS pixels on a drawing area <see cref="ReferenceWidth"/> pixels wide, and the line is drawn
/// proportionally wider or thinner on a wider or narrower area. On export (see
/// <see cref="OmniInkCanvas.ExportPngAsync"/>) an image of <c>w</c> pixels wide draws the line
/// <c>w / ReferenceWidth</c> times <see cref="Width"/> pixels wide.
/// </para>
/// <para>
/// The strokes are values: the canvas never changes one, it replaces the list it holds. Record equality compares
/// <see cref="Color"/>, <see cref="Width"/>, <see cref="Id"/> and <see cref="Points"/>, the last by reference as for any
/// list: two strokes drawn on a canvas are never equal (each has its own <see cref="Id"/>), and a copy of a stroke
/// made with <c>with</c> is equal to it only while it shares the same point list. To recognise a stroke across
/// browsers or lists, compare <see cref="Id"/>, not the record.
/// </para>
/// </remarks>
/// <param name="Color">
/// A CSS colour (<c>#1a73e8</c>, <c>rgb(0 0 0)</c>, <c>navy</c>), or <see cref="ThemeColor"/> for the text colour of
/// the theme the canvas sits in, which follows a change of theme.
/// </param>
/// <param name="Width">The width of the line in CSS pixels at <see cref="ReferenceWidth"/>; above zero.</param>
/// <param name="Points">The sampled points, in the order they were put down; at least one.</param>
public sealed record OmniInkStroke(string Color, double Width, IReadOnlyList<OmniInkPoint> Points)
{
    /// <summary>
    /// The colour of a stroke drawn without an explicit colour: the text colour of the theme (<c>currentColor</c>).
    /// A PNG export has no theme, so these strokes are written in black there.
    /// </summary>
    public const string ThemeColor = "currentColor";

    /// <summary>The drawing area width, in CSS pixels, at which <see cref="Width"/> is expressed: 600.</summary>
    public const double ReferenceWidth = 600;

    /// <summary>
    /// An identifier of the stroke, for a host that transports strokes between browsers and needs to tell them apart.
    /// The canvas gives every stroke it draws a fresh one (a GUID in its 32-digit form); a stroke the host builds keeps
    /// the identifier the host gives it, or none (null, the default). The canvas does not require identifiers to be
    /// unique and never rewrites one; it only uses those of the strokes it drew itself to undo them first.
    /// </summary>
    public string? Id { get; init; }
}
