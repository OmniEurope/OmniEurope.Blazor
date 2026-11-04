namespace OmniEurope.Blazor.Components;

/// <summary>A stretch of text of a paragraph, edited in place as plain text.</summary>
/// <param name="Id">The identifier of the run, unique among the runs and fields of the document.</param>
/// <param name="Text">The text, on one line: a line break in it is edited as a space. May be empty.</param>
/// <param name="StyleKey">
/// An opaque key owned by the host (a character style, a font): the editor never reads it, it only hands it to
/// <see cref="OmniDocumentEditor.StyleClasses"/> to look up the CSS class the run is drawn with. Null for none.
/// </param>
public sealed record OmniDocumentRun(string Id, string Text, string? StyleKey = null) : OmniDocumentInline(Id);
