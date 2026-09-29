namespace OmniEurope.Blazor.Components;

/// <summary>
/// What <see cref="OmniCheckBoxList{TValue}"/> and <see cref="OmniRadioButtonList{TValue}"/> share, so the
/// two lists keep the same classes and the same description order.
/// </summary>
internal static class OmniChoiceListParts
{
    /// <summary>The class of one choice: dimmed and marked unavailable when the option is disabled.</summary>
    internal static string ItemClass(bool disabled) => disabled
        ? "omni-choice-list__item omni-choice-list__item--disabled"
        : "omni-choice-list__item";

    /// <summary>The consumer's description first, then the error line when there is one; null when neither.</summary>
    internal static string? DescribedBy(string? describedBy, string? errorId)
    {
        var passed = string.IsNullOrWhiteSpace(describedBy) ? null : describedBy;
        return errorId is null ? passed : passed is null ? errorId : $"{passed} {errorId}";
    }
}
