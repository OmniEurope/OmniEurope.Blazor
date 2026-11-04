namespace OmniEurope.Blazor.Components;

/// <summary>
/// The document an <see cref="OmniDocumentEditor"/> edits: a list of paragraphs. The model is neutral on purpose:
/// the package knows no document format, so the host converts its own document to these values and applies the
/// <see cref="OmniDocumentChangeSet"/> the editor reports back to it.
/// </summary>
/// <remarks>The editor never changes a document it was given; it reads it again when the parameter is given another instance.</remarks>
/// <param name="Blocks">The paragraphs, in reading order. Block ids are unique within the document.</param>
public sealed record OmniDocument(IReadOnlyList<OmniDocumentBlock> Blocks);
