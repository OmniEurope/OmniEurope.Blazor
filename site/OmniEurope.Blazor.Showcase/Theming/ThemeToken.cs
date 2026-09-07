namespace OmniEurope.Blazor.Showcase.Theming;

/// <summary>
/// One custom property read from the library stylesheet, with the value it ships with.
/// </summary>
/// <param name="Name">The property name, dashes included, for example <c>--omni-color-accent</c>.</param>
/// <param name="DefaultValue">The value declared in the stylesheet, used to reset the token.</param>
/// <param name="Group">The family the editor files the token under.</param>
public sealed record ThemeToken(string Name, string DefaultValue, ThemeTokenGroup Group)
{
    /// <summary>
    /// The name without the <c>--omni-</c> prefix, which is what the editor shows as a label.
    /// </summary>
    public string Label => Name.StartsWith("--omni-", StringComparison.Ordinal)
        ? Name["--omni-".Length..].Replace('-', ' ')
        : Name;

    /// <summary>
    /// Whether the value is a colour the editor can hand to a colour picker. Shadows and gradients
    /// carry colours too, but they are compound values that only a text field can edit safely.
    /// </summary>
    public bool IsColor => DefaultValue.StartsWith('#')
        && DefaultValue.Length is 4 or 7
        && DefaultValue[1..].All(Uri.IsHexDigit);
}
