namespace OmniEurope.Blazor.Components;

/// <summary>
/// What the reader changed in an <see cref="OmniDocumentEditor"/> since the document it was given: the text of runs,
/// the values of fields and the drawings of paragraphs, each in the order of the document. A run or field put back to
/// what the document says is not in it; a drawing replaced twice is there once, as it stands.
/// </summary>
/// <param name="Runs">The runs whose text differs from the document.</param>
/// <param name="Fields">The fields whose value differs from the document.</param>
/// <param name="Drawings">The paragraphs the reader drew for.</param>
public sealed record OmniDocumentChangeSet(
    IReadOnlyList<OmniRunTextChange> Runs,
    IReadOnlyList<OmniFieldValueChange> Fields,
    IReadOnlyList<OmniDrawingChange> Drawings)
{
    /// <summary>Whether nothing was changed: no run, no field and no drawing.</summary>
    public bool IsEmpty => Runs.Count == 0 && Fields.Count == 0 && Drawings.Count == 0;
}
