using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The free width of a dialog: a CSS length the script writes as a custom property, so it is checked
/// here to be nothing but a number and a unit (no expression, no second declaration, no url).
/// </summary>
internal static partial class DialogWidth
{
    /// <summary>Throws <see cref="ArgumentException"/> unless <paramref name="width"/> is null or a number followed by px, rem, em, ch, vw or %.</summary>
    internal static void Validate(string? width, string parameterName)
    {
        if (width is not null && !Length().IsMatch(width))
        {
            throw new ArgumentException($"'{width}' is not a dialog width (a positive number followed by px, rem, em, ch, vw or %).", parameterName);
        }
    }

    [GeneratedRegex("^(?:[1-9]\\d{0,4}(?:\\.\\d{1,3})?|0\\.\\d{0,2}[1-9])(?:px|rem|em|ch|vw|%)$", RegexOptions.CultureInvariant)]
    private static partial Regex Length();
}
