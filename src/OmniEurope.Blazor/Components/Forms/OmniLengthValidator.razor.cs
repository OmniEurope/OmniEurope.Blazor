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
    public int? Min { get; set; }

    /// <summary>The most characters the text may hold; null sets no maximum.</summary>
    [Parameter]
    public int? Max { get; set; }

    /// <summary>
    /// The message of a text too short or too long. Null or blank, the default, writes a localized
    /// sentence with the bounds: at least <see cref="Min"/>, at most <see cref="Max"/>, between both,
    /// or exactly one length when they are equal.
    /// </summary>
    [Parameter]
    public string? Message { get; set; }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> when neither <see cref="Min"/> nor <see cref="Max"/> is set,
    /// when one is negative, or when <see cref="Min"/> exceeds <see cref="Max"/>; then follows the field.
    /// </summary>
    protected override void OnParametersSet()
    {
        if (Min is null && Max is null)
        {
            throw new ArgumentException($"{nameof(OmniLengthValidator)} needs {nameof(Min)}, {nameof(Max)} or both.", nameof(Min));
        }

        if (Min < 0 || Max < 0)
        {
            throw new ArgumentException($"{nameof(OmniLengthValidator)} bounds cannot be negative.", Min < 0 ? nameof(Min) : nameof(Max));
        }

        if (Min > Max)
        {
            throw new ArgumentException($"{nameof(OmniLengthValidator)}.{nameof(Min)} ({Min}) exceeds {nameof(Max)} ({Max}).", nameof(Min));
        }

        base.OnParametersSet();
    }

    /// <inheritdoc />
    protected override string? GetValidationError(string? value)
    {
        if (string.IsNullOrEmpty(value) || (value.Length >= (Min ?? 0) && value.Length <= (Max ?? int.MaxValue)))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(Message))
        {
            return Message;
        }

        return (Min, Max) switch
        {
            ({ } min, { } max) when min == max => Localize("LengthValidatorExact", min),
            ({ } min, { } max) => Localize("LengthValidatorRange", min, max),
            ({ } min, null) => Localize("LengthValidatorMinimum", min),
            _ => Localize("LengthValidatorMaximum", Max!.Value)
        };
    }
}
