namespace OmniEurope.Blazor.Components;

/// <summary>A compact, keyboard-accessible star rating, also available as a read-only display.</summary>
public partial class OmniRating
{
    /// <summary>The number of stars, and the highest rating; five by default, at least one.</summary>
    [Parameter] public int Maximum { get; set; } = 5;

    /// <summary>
    /// Whether the rating is shown without being editable: one focusable image that announces the value
    /// (for example "Note 3/5"), with its stars drawn at full strength. <see cref="Disabled"/> wins over it.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Whether the stars are disabled buttons, out of the tab order and dimmed.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Names the group and each star, and prefixes the value announced when read-only.</summary>
    [Parameter] public string Label { get; set; } = string.Empty;

    private bool IsReadOnlyDisplay => ReadOnly && !Disabled;

    private string StarLabel(int star) => $"{Label} {star}/{Maximum}";

    private string ValueLabel => $"{Label} {CurrentValue ?? 0}/{Maximum}".TrimStart();

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
