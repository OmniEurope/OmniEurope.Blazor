using System.Globalization;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The one way a grid turns a cell value into text: what the cell shows, what the fit to content measures
/// and what an export writes all go through it, so the three can never read the same value differently.
/// </summary>
internal static class GridCellFormat
{
    /// <summary>
    /// The text of <paramref name="value"/> in the current culture, through the composite
    /// <paramref name="formatString"/> (for example <c>{0:n2} EUR</c>) when there is one. A pattern the
    /// framework refuses (a malformed composite, a specifier the value does not know) shows the value as
    /// it is, instead of failing a render or a whole export for one column.
    /// </summary>
    /// <param name="value">The cell value; null reads as an empty text.</param>
    /// <param name="formatString">The column's composite format, or null or blank for none.</param>
    /// <returns>The text, never null.</returns>
    internal static string Text(object? value, string? formatString)
    {
        if (!string.IsNullOrWhiteSpace(formatString))
        {
            try
            {
                return string.Format(CultureInfo.CurrentCulture, formatString, value);
            }
            catch (FormatException)
            {
            }
        }

        return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
    }
}
