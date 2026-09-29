namespace OmniEurope.Blazor.Components;

/// <summary>
/// A colour field: the browser's colour input, bound to a <c>#RRGGBB</c> string, with the value
/// written beside it.
/// </summary>
public partial class OmniColorPicker
{
    /// <summary>Whether the field is disabled. Off by default.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the value is written beside the colour input, as an <c>output</c>; true by default.</summary>
    [Parameter]
    public bool ShowValue { get; set; } = true;

    /// <summary>Identifiers of the elements that describe the field, written as <c>aria-describedby</c>; none when null.</summary>
    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private void HandleInput(ChangeEventArgs args) => CurrentValueAsString = args.Value?.ToString();

    /// <summary>Accepts a <c>#</c> followed by six hexadecimal digits, stored in capitals.</summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="result">The colour in capitals; on failure, the current value, or <c>#000000</c> when it is null.</param>
    /// <param name="validationErrorMessage">Null on success; on failure, the localized message asking for the #RRGGBB format.</param>
    /// <returns>True when the text is a <c>#RRGGBB</c> colour.</returns>
    protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
    {
        if (value is { Length: 7 } && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit))
        {
            result = value.ToUpperInvariant();
            validationErrorMessage = null!;
            return true;
        }

        result = CurrentValue ?? "#000000";
        validationErrorMessage = Localize("ColorPickerInvalid");
        return false;
    }
}
