using System.Globalization;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Cell addresses in the A1 notation: letters for the column (A to Z, then AA, AB and so on) and a
/// one-based number for the row. Positions are zero-based everywhere else in the code.
/// </summary>
internal static class SpreadsheetAddress
{
    internal static string ColumnName(int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        var name = string.Empty;
        var remaining = column + 1;
        while (remaining > 0)
        {
            var digit = (remaining - 1) % 26;
            name = (char)('A' + digit) + name;
            remaining = (remaining - 1) / 26;
        }

        return name;
    }

    internal static string Format(int row, int column) =>
        string.Create(CultureInfo.InvariantCulture, $"{ColumnName(column)}{row + 1}");

    /// <summary>
    /// Reads an address such as <c>B3</c> or <c>$B$3</c>, whatever the case of its letters. Fails
    /// on anything else, including a row number of zero.
    /// </summary>
    internal static bool TryParse(ReadOnlySpan<char> text, out int row, out int column)
    {
        row = -1;
        column = -1;
        var index = 0;
        if (index < text.Length && text[index] == '$')
        {
            index++;
        }

        var letters = 0;
        var columnNumber = 0;
        while (index < text.Length && char.IsAsciiLetter(text[index]))
        {
            columnNumber = checked((columnNumber * 26) + (char.ToUpperInvariant(text[index]) - 'A' + 1));
            letters++;
            index++;
            if (letters > 3)
            {
                return false;
            }
        }

        if (letters == 0)
        {
            return false;
        }

        if (index < text.Length && text[index] == '$')
        {
            index++;
        }

        var digitsStart = index;
        while (index < text.Length && char.IsAsciiDigit(text[index]))
        {
            index++;
        }

        if (index != text.Length
            || index == digitsStart
            || index - digitsStart > 7
            || !int.TryParse(text[digitsStart..index], NumberStyles.None, CultureInfo.InvariantCulture, out var rowNumber)
            || rowNumber < 1)
        {
            return false;
        }

        row = rowNumber - 1;
        column = columnNumber - 1;
        return true;
    }
}
