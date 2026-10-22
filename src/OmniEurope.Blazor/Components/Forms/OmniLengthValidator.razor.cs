namespace OmniEurope.Blazor.Components;

/// <summary>
/// A validator that bounds the length of a text field, when it holds anything: a null or empty value
/// passes, so a required text pairs it with <see cref="OmniRequiredValidator{TValue}"/>.
/// </summary>
/// <remarks>
/// The length is counted in UTF-16 units (<see cref="string.Length"/>), as the <c>maxlength</c>
/// attribute of an input and the <c>StringLength</c> attribute of a server model count it, so the
/// field and the server agree; a character outside the basic plane (most emoji) counts for two.
/// Spaces count.
/// </remarks>
public partial class OmniLengthValidator
{
    /// <summary>The fewest characters the text may hold; null sets no minimum.</summary>
    [Parameter]
    public int? Minimum { get; set; }

    /// <summary>The most characters the text may hold; null sets no maximum.</summary>
    [Parameter]
    public int? Maximum { get; set; }

    /// <summary>
    /// The message of a text too short or too long. Null or blank, the default, writes a localized
    /// sentence with the bounds: at least <see cref="Minimum"/>, at most <see cref="Maximum"/>, between both,
    /// or exactly one length when they are equal.
    /// </summary>
    [Parameter]
    public string? Message { get; set; }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> when neither <see cref="Minimum"/> nor <see cref="Maximum"/> is set,
    /// when one is negative, or when <see cref="Minimum"/> exceeds <see cref="Maximum"/>; then follows the field.
    /// </summary>
    protected override void OnParametersSet()
    {
        if (Minimum is null && Maximum is null)
        {
            throw new ArgumentException($"{nameof(OmniLengthValidator)} needs {nameof(Minimum)}, {nameof(Maximum)} or both.", nameof(Minimum));
        }

        if (Minimum < 0 || Maximum < 0)
        {
            throw new ArgumentException($"{nameof(OmniLengthValidator)} bounds cannot be negative.", Minimum < 0 ? nameof(Minimum) : nameof(Maximum));
        }

        if (Minimum > Maximum)
        {
            throw new ArgumentException($"{nameof(OmniLengthValidator)}.{nameof(Minimum)} ({Minimum}) exceeds {nameof(Maximum)} ({Maximum}).", nameof(Minimum));
        }

        base.OnParametersSet();
    }

    /// <inheritdoc />
    protected override string? GetValidationError(string? value)
    {
        if (string.IsNullOrEmpty(value) || (value.Length >= (Minimum ?? 0) && value.Length <= (Maximum ?? int.MaxValue)))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(Message))
        {
            return Message;
        }

        return (Minimum, Maximum) switch
        {
            ({ } min, { } max) when min == max => Localize("LengthValidatorExact", min),
            ({ } min, { } max) => Localize("LengthValidatorRange", min, max),
            ({ } min, null) => Localize("LengthValidatorMinimum", min),
            _ => Localize("LengthValidatorMaximum", Maximum!.Value)
        };
    }
}
