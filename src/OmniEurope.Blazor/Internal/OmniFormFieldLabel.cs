namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What an <see cref="Components.OmniFormField"/> cascades to its control: the id its label names
/// (<c>For</c>) and the id of the label itself. A control that is not a native labelable element (an
/// editable <c>div</c>, a radio group, a fieldset without legend), which a <c>label for</c> cannot name,
/// points <c>aria-labelledby</c> at the label instead.
/// </summary>
/// <param name="ControlId">The id the label names.</param>
/// <param name="LabelId">The id of the label element.</param>
internal sealed record OmniFormFieldLabel(string ControlId, string LabelId)
{
    /// <summary>
    /// The label id when <paramref name="controlId"/> is the control the label names, else null: a
    /// control only takes the name of a label written for it.
    /// </summary>
    internal static string? For(OmniFormFieldLabel? field, string? controlId) =>
        field is not null && !string.IsNullOrWhiteSpace(controlId) && string.Equals(field.ControlId, controlId, StringComparison.Ordinal)
            ? field.LabelId
            : null;
}
