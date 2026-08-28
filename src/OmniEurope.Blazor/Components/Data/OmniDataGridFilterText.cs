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

    /// <summary>The comparison implied by a case-sensitivity setting.</summary>
    public static StringComparison Comparison(OmniDataGridFilterCaseSensitivity caseSensitivity) =>
        caseSensitivity == OmniDataGridFilterCaseSensitivity.CaseSensitive
            ? StringComparison.CurrentCulture
            : StringComparison.CurrentCultureIgnoreCase;
}
