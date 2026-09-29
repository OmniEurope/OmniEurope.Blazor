using System.Globalization;
using System.Text;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Text normalization the grid applies to both sides of every filter comparison. Public so a host
/// that filters its own rows behind a <c>Load</c> callback can match the grid's semantics exactly
/// instead of approximating them.
/// </summary>
public static class OmniDataGridFilterText
{
    /// <summary>
    /// Strips diacritics when asked. Decomposes the string and drops the combining marks, so an
    /// accented letter becomes its plain form and a search for "epee" finds the accented spelling.
    /// </summary>
    /// <param name="value">The text to normalize; null reads as empty.</param>
    /// <param name="ignoreDiacritics">True to strip diacritics (the grid's <c>IgnoreDiacritics</c>); false returns the text unchanged.</param>
    /// <returns>The normalized text, never null.</returns>
    public static string Normalize(string? value, bool ignoreDiacritics)
    {
        if (string.IsNullOrEmpty(value) || !ignoreDiacritics)
        {
            return value ?? string.Empty;
        }

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// The comparison the grid uses for a filter: the current culture, ignoring case unless
    /// <paramref name="caseSensitive"/> (the grid's <c>CaseSensitiveFilters</c>) is true.
    /// </summary>
    /// <param name="caseSensitive">True for a case-sensitive comparison.</param>
    /// <returns><see cref="StringComparison.CurrentCulture"/> or <see cref="StringComparison.CurrentCultureIgnoreCase"/>.</returns>
    public static StringComparison Comparison(bool caseSensitive) =>
        caseSensitive
            ? StringComparison.CurrentCulture
            : StringComparison.CurrentCultureIgnoreCase;
}
