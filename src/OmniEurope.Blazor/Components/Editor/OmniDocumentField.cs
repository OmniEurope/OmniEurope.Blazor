namespace OmniEurope.Blazor.Components;

/// <summary>
/// A value of the document the reader fills in, drawn inside the paragraph as a chip that shows its label and its
/// value and opens the control matching its <see cref="Kind"/> when activated.
/// </summary>
/// <param name="Id">The identifier of the field, unique among the runs and fields of the document.</param>
/// <param name="Tag">What the field stands for in the host's document (a merge field name, a content control tag); several fields may share a tag.</param>
/// <param name="Label">The name the reader sees on the chip and that names the control; not empty.</param>
/// <param name="Kind">The kind of value the field holds.</param>
/// <param name="Value">
/// The value as a string, null or empty for none: <c>yyyy-MM-dd</c> for a <see cref="OmniDocumentFieldKind.Date"/>, the invariant
/// culture for a <see cref="OmniDocumentFieldKind.Number"/>, one of <paramref name="Items"/> for a
/// <see cref="OmniDocumentFieldKind.List"/> unless <paramref name="AllowFreeText"/>. A value that does not fit its kind is refused.
/// </param>
/// <param name="Items">The choices of a <see cref="OmniDocumentFieldKind.List"/>, distinct and not empty unless <paramref name="AllowFreeText"/>; null for the other kinds.</param>
/// <param name="AllowFreeText">For a <see cref="OmniDocumentFieldKind.List"/>, whether the reader may type a value that is not one of the items; false for the other kinds.</param>
public sealed record OmniDocumentField(
    string Id,
    string Tag,
    string Label,
    OmniDocumentFieldKind Kind,
    string? Value = null,
    IReadOnlyList<string>? Items = null,
    bool AllowFreeText = false) : OmniDocumentInline(Id);
