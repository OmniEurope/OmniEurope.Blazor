namespace OmniEurope.Blazor.Components;

/// <summary>The new value of a field the reader changed.</summary>
/// <param name="FieldId">The <see cref="OmniDocumentInline.Id"/> of the field.</param>
/// <param name="Tag">The <see cref="OmniDocumentField.Tag"/> of the field, so that the host need not look it up.</param>
/// <param name="Value">The value now in the field, in the format of its kind (see <see cref="OmniDocumentFieldKind"/>); null when the reader emptied it.</param>
public sealed record OmniFieldValueChange(string FieldId, string Tag, string? Value);
