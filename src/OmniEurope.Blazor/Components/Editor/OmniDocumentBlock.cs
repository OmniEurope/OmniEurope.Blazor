namespace OmniEurope.Blazor.Components;

/// <summary>One paragraph of an <see cref="OmniDocument"/>: runs of text and fields in a line, and at most one drawing.</summary>
/// <param name="Id">The identifier of the paragraph, unique among the paragraphs of the document; the change set names a drawing by it.</param>
/// <param name="Inlines">The runs and fields of the paragraph, in reading order. Inline ids are unique within the document.</param>
/// <param name="Drawing">The drawing shown under the paragraph, or null when it has none.</param>
public sealed record OmniDocumentBlock(string Id, IReadOnlyList<OmniDocumentInline> Inlines, OmniDocumentDrawing? Drawing = null);
