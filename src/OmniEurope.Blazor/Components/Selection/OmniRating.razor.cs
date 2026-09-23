namespace OmniEurope.Blazor.Components;

/// <summary>A compact, keyboard-accessible star rating, also available as a read-only display.</summary>
public partial class OmniRating
{
    [Parameter] public int Maximum { get; set; } = 5;
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public string Label { get; set; } = string.Empty;

    private string StarLabel(int star) => $"{Label} {star}/{Maximum}";

    private void Select(int star)
    {
        if (!Disabled && !ReadOnly) CurrentValue = star;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentOutOfRangeException.ThrowIfLessThan(Maximum, 1);
    }

    protected override bool TryParseValueFromString(string? value, out int? result, out string validationErrorMessage)
    {
        result = int.TryParse(value, out var parsed) ? parsed : null;
        var valid = string.IsNullOrEmpty(value) || result is >= 0 && result <= Maximum;
        validationErrorMessage = valid ? string.Empty : Localize("NumericInvalid");
        return valid;
    }
}
