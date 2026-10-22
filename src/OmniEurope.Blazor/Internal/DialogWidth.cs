namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The free width of a dialog: a CSS length the script writes as a custom property, so it is checked
/// here to be nothing but a positive number and a unit (no expression, no second declaration, no url).
/// </summary>
internal static class DialogWidth
{
    /// <summary>Throws <see cref="ArgumentException"/> unless <paramref name="width"/> is null or a number above zero followed by px, rem, em, ch, vw or %.</summary>
    internal static void Validate(string? width, string parameterName)
    {
        if (width is not null && !CssLength.IsWidth(width, positive: true))
        {
            throw new ArgumentException($"'{width}' is not a dialog width (a positive number followed by px, rem, em, ch, vw or %).", parameterName);
        }
    }
}
