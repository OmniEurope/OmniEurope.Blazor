namespace OmniEurope.Blazor.Components;

/// <summary>
/// A compact star rating, a radio group of <see cref="Maximum"/> stars, also available as a read-only
/// display.
/// </summary>
/// <remarks>
/// The stars are native radio buttons hidden behind their glyphs: the group has one tab stop, the
/// arrow keys move to the next or previous star and choose it, and each star is announced as
/// "<c>n</c> of <c>N</c>" within the group named by <see cref="Label"/>.
/// </remarks>
public partial class OmniRating
{
    private readonly string _generatedName = $"omni-rating-{Guid.NewGuid():N}";

    /// <summary>The number of stars, and the highest rating; five by default, at least one.</summary>
    [Parameter] public int Maximum { get; set; } = 5;

    /// <summary>
    /// Whether the rating is shown without being editable: one image that announces the value (for
    /// example "Note : 3 sur 5"), outside the tab order, with its stars drawn at full strength.
    /// <see cref="Disabled"/> wins over it.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Whether the stars are disabled radio buttons, out of the tab order and dimmed.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Names the group, and prefixes the value announced when read-only. Null uses the label of an
    /// enclosing <see cref="OmniFormField"/> whose <c>For</c> is <see cref="OmniInputBase{TValue}.Id"/>
    /// (through <c>aria-labelledby</c>, a radio group being out of reach of a <c>label for</c>), else the
    /// localized "Rating".
    /// </summary>
    [Parameter] public string? Label { get; set; }

    private string? GroupLabelledBy => string.IsNullOrWhiteSpace(Label) ? FormFieldLabelId : null;

    private bool IsReadOnlyDisplay => ReadOnly && !Disabled;

    private string GroupName => Id ?? _generatedName;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("RatingLabel") : Label;

    private string StarLabel(int star) => Localize("RatingStar", star, Maximum);

    private string ValueLabel => Localize("RatingValue", EffectiveLabel, CurrentValue ?? 0, Maximum);

    private void Select(int star)
    {
        // Read-only without Disabled draws no radio at all: only a disabled rating has stars to refuse.
        if (!Disabled) CurrentValue = star;
    }

    /// <summary>Checks the parameters.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="Maximum"/> is less than 1.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentOutOfRangeException.ThrowIfLessThan(Maximum, 1);
    }

    /// <summary>Reads a whole number from 0 to <see cref="Maximum"/>; empty text means no rating.</summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="result">The rating read, or null when the text is not a number.</param>
    /// <param name="validationErrorMessage">Empty on success; on failure, the localized "invalid number" message.</param>
    /// <returns>True when the text is empty or a rating within the range.</returns>
    protected override bool TryParseValueFromString(string? value, out int? result, out string validationErrorMessage)
    {
        result = int.TryParse(value, out var parsed) ? parsed : null;
        var valid = string.IsNullOrEmpty(value) || result is >= 0 && result <= Maximum;
        validationErrorMessage = valid ? string.Empty : Localize("NumericInvalid");
        return valid;
    }
}
