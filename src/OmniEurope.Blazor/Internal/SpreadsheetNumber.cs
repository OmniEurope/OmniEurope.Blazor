using System.Globalization;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// How a spreadsheet text reads as a number, shared by a cell's input and a formula's text operand: in
/// the current culture or with a point, a trailing <c>%</c> dividing by a hundred.
/// </summary>
internal static class SpreadsheetNumber
{
    internal static bool TryRead(string text, out double number)
    {
        var trimmed = text.Trim();
        var percent = trimmed.EndsWith('%');
        if (percent)
        {
            trimmed = trimmed[..^1].TrimEnd();
        }

        const NumberStyles styles = NumberStyles.Float;
        if (double.TryParse(trimmed, styles, CultureInfo.CurrentCulture, out number)
            || double.TryParse(trimmed, styles, CultureInfo.InvariantCulture, out number))
        {
            if (percent)
            {
                number /= 100d;
            }

            return true;
        }

        number = 0d;
        return false;
    }
}
