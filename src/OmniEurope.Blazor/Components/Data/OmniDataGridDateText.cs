using System.Globalization;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The text a <see cref="OmniDataGridColumnFilterType.Date"/> filter reads: one day, one month or one
/// year, written the way the reader's culture writes dates. <see cref="TryParse"/> turns the text into a
/// half-open period, <see cref="ToValue"/> into the encoded range the grid keeps
/// (<see cref="OmniDataGridDateRange"/>), and <see cref="Format"/> and <see cref="Pattern"/> write them
/// back. Nothing here reads the clock or the current culture: the culture is always passed in.
/// </summary>
/// <remarks>
/// Accepted forms, with <c>/</c>, <c>.</c> or <c>-</c> between the numbers, spaces around them ignored:
/// a day (<c>05/03/2026</c>), a month (<c>03/2026</c>) and a year (<c>2026</c>). The day and the month
/// come in the order of the culture's short date pattern (day first when the culture says nothing
/// else); a year written first (<c>2026-03-05</c>, <c>2026/03</c>) is read in year, month, day order
/// whatever the culture. The year always has four digits (0001 to 9999): a two-digit year, a five-digit
/// year, a day the month does not have (<c>31/02/2026</c>) or any other text is not a date. There is no
/// time part.
/// </remarks>
public static class OmniDataGridDateText
{
    private const int YearDigits = 4;

    private static readonly char[] Separators = ['/', '.', '-'];

    private enum DateOrder
    {
        DayMonthYear,
        MonthDayYear,
        YearMonthDay
    }

    /// <summary>Reads a day, a month or a year as the period it covers.</summary>
    /// <param name="text">The text typed; null, empty or white space is not a date.</param>
    /// <param name="culture">The culture whose date order the day and the month follow.</param>
    /// <param name="start">The first instant of the period (midnight of its first day) when the text is a date.</param>
    /// <param name="endExclusive">
    /// The first instant after the period: midnight of the next day, month or year. For a period ending
    /// on 31 December 9999, the last representable instant.
    /// </param>
    /// <returns>True when the text is a valid day, month or year.</returns>
    public static bool TryParse(string? text, CultureInfo culture, out DateTime start, out DateTime endExclusive)
    {
        ArgumentNullException.ThrowIfNull(culture);
        start = default;
        endExclusive = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text.Split(Separators).Select(part => part.Trim()).ToArray();
        if (parts.Length is < 1 or > 3 || !parts.All(IsNumber))
        {
            return false;
        }

        return parts.Length switch
        {
            1 => TryYear(parts[0], out start, out endExclusive),
            2 => TryMonth(parts, out start, out endExclusive),
            _ => TryDay(parts, culture, out start, out endExclusive)
        };
    }

    /// <summary>The encoded range the grid keeps for a period, whole days from <paramref name="start"/> to the last day before <paramref name="endExclusive"/>.</summary>
    /// <param name="start">The first instant of the period, as <see cref="TryParse"/> returns it.</param>
    /// <param name="endExclusive">The first instant after the period.</param>
    /// <returns>The <see cref="OmniDataGridDateRange.Join"/> value, for example <c>2026-03-01/2026-03-31</c>.</returns>
    public static string ToValue(DateTime start, DateTime endExclusive) =>
        OmniDataGridDateRange.Join(Iso(start), Iso(endExclusive.AddTicks(-1)));

    /// <summary>
    /// The text that shows an encoded range back: a day, a month or a year when the range is exactly one,
    /// else both days with a hyphen between them, an open side left empty. Not meant to be parsed again.
    /// </summary>
    /// <param name="value">The encoded range (<see cref="OmniDataGridDateRange"/>); null or empty gives an empty text.</param>
    /// <param name="culture">The culture whose date order and separator the text follows.</param>
    /// <returns>The text, or an empty string when the range has no readable side.</returns>
    public static string Format(string? value, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        var (start, endExclusive) = OmniDataGridDateRange.Resolve(value);
        if (start is not { } from)
        {
            return endExclusive is { } until ? $"- {Day(until.AddTicks(-1), culture)}" : string.Empty;
        }

        if (endExclusive is not { } to)
        {
            return $"{Day(from, culture)} -";
        }

        // Whole days from midnight to a next day, month or year; the one ending in 9999 ends on the last instant.
        if (from.TimeOfDay == TimeSpan.Zero)
        {
            if (to == NextDay(from))
            {
                return Day(from, culture);
            }

            if (from.Day == 1 && to == NextMonth(from))
            {
                return Month(from, culture);
            }

            if (from is { Month: 1, Day: 1 } && to == NextYear(from))
            {
                return from.Year.ToString("D4", CultureInfo.InvariantCulture);
            }
        }

        return $"{Day(from, culture)} - {Day(to.AddTicks(-1), culture)}";
    }

    /// <summary>
    /// The hint of the format, in the order and with the separator of the culture, written with the given
    /// letters: <c>dd/mm/yyyy</c> for a day-first culture, <c>mm/dd/yyyy</c> for a month-first one,
    /// <c>yyyy-mm-dd</c> for a year-first one; <c>jj/mm/aaaa</c> with the French letters.
    /// </summary>
    /// <param name="culture">The culture whose date order and separator the hint follows.</param>
    /// <param name="day">The letters standing for the day, in the language of the reader (<c>dd</c>, <c>jj</c>).</param>
    /// <param name="month">The letters standing for the month (<c>mm</c>).</param>
    /// <param name="year">The letters standing for the year (<c>yyyy</c>, <c>aaaa</c>).</param>
    /// <returns>The hint, for example a placeholder.</returns>
    public static string Pattern(CultureInfo culture, string day, string month, string year)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentException.ThrowIfNullOrWhiteSpace(day);
        ArgumentException.ThrowIfNullOrWhiteSpace(month);
        ArgumentException.ThrowIfNullOrWhiteSpace(year);
        var separator = SeparatorOf(culture);
        return OrderOf(culture) switch
        {
            DateOrder.MonthDayYear => $"{month}{separator}{day}{separator}{year}",
            DateOrder.YearMonthDay => $"{year}{separator}{month}{separator}{day}",
            _ => $"{day}{separator}{month}{separator}{year}"
        };
    }

    private static bool TryYear(string text, out DateTime start, out DateTime endExclusive)
    {
        start = default;
        endExclusive = default;
        if (text.Length != YearDigits || !TryNumber(text, 1, 9999, out var year))
        {
            return false;
        }

        start = new DateTime(year, 1, 1);
        endExclusive = NextYear(start);
        return true;
    }

    private static bool TryMonth(string[] parts, out DateTime start, out DateTime endExclusive)
    {
        start = default;
        endExclusive = default;
        // The year is the part with four digits, on either side, whatever the culture's order.
        var yearFirst = parts[0].Length == YearDigits;
        var (yearText, monthText) = yearFirst ? (parts[0], parts[1]) : (parts[1], parts[0]);
        if (yearText.Length != YearDigits
            || monthText.Length > 2
            || !TryNumber(yearText, 1, 9999, out var year)
            || !TryNumber(monthText, 1, 12, out var month))
        {
            return false;
        }

        start = new DateTime(year, month, 1);
        endExclusive = NextMonth(start);
        return true;
    }

    private static bool TryDay(string[] parts, CultureInfo culture, out DateTime start, out DateTime endExclusive)
    {
        start = default;
        endExclusive = default;
        string yearText;
        string monthText;
        string dayText;
        if (parts[0].Length == YearDigits)
        {
            (yearText, monthText, dayText) = (parts[0], parts[1], parts[2]);
        }
        else if (OrderOf(culture) == DateOrder.MonthDayYear)
        {
            (monthText, dayText, yearText) = (parts[0], parts[1], parts[2]);
        }
        else
        {
            (dayText, monthText, yearText) = (parts[0], parts[1], parts[2]);
        }

        if (yearText.Length != YearDigits
            || monthText.Length > 2
            || dayText.Length > 2
            || !TryNumber(yearText, 1, 9999, out var year)
            || !TryNumber(monthText, 1, 12, out var month)
            || !TryNumber(dayText, 1, DateTime.DaysInMonth(year, month), out var day))
        {
            return false;
        }

        start = new DateTime(year, month, day);
        endExclusive = NextDay(start);
        return true;
    }

    // Only ASCII digits: the digits of another script would pass int.TryParse in some cultures.
    private static bool IsNumber(string text) => text.Length > 0 && text.All(char.IsAsciiDigit);

    private static bool TryNumber(string text, int minimum, int maximum, out int value) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value >= minimum && value <= maximum;

    // The first instant after a day, a month, a year: the last representable instant for the last of 9999.
    private static DateTime NextDay(DateTime value) =>
        value.Date == DateTime.MaxValue.Date ? DateTime.MaxValue : value.AddDays(1);

    private static DateTime NextMonth(DateTime value) =>
        value.Year == 9999 && value.Month == 12 ? DateTime.MaxValue : value.AddMonths(1);

    private static DateTime NextYear(DateTime value) =>
        value.Year == 9999 ? DateTime.MaxValue : value.AddYears(1);

    private static string Iso(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Day(DateTime value, CultureInfo culture)
    {
        var separator = SeparatorOf(culture);
        var year = value.Year.ToString("D4", CultureInfo.InvariantCulture);
        var month = value.Month.ToString("D2", CultureInfo.InvariantCulture);
        var day = value.Day.ToString("D2", CultureInfo.InvariantCulture);
        return OrderOf(culture) switch
        {
            DateOrder.MonthDayYear => $"{month}{separator}{day}{separator}{year}",
            DateOrder.YearMonthDay => $"{year}{separator}{month}{separator}{day}",
            _ => $"{day}{separator}{month}{separator}{year}"
        };
    }

    private static string Month(DateTime value, CultureInfo culture)
    {
        var separator = SeparatorOf(culture);
        var year = value.Year.ToString("D4", CultureInfo.InvariantCulture);
        var month = value.Month.ToString("D2", CultureInfo.InvariantCulture);
        return OrderOf(culture) == DateOrder.YearMonthDay
            ? $"{year}{separator}{month}"
            : $"{month}{separator}{year}";
    }

    // The culture's own separator when the parser reads it, else the slash.
    private static string SeparatorOf(CultureInfo culture)
    {
        var separator = culture.DateTimeFormat.DateSeparator;
        return separator.Length == 1 && Array.IndexOf(Separators, separator[0]) >= 0 ? separator : "/";
    }

    // Read from where the short date pattern puts the year, the month and the day.
    private static DateOrder OrderOf(CultureInfo culture)
    {
        var pattern = culture.DateTimeFormat.ShortDatePattern;
        var year = Position(pattern, 'y');
        var month = Position(pattern, 'M');
        var day = Position(pattern, 'd');
        if (year < month && year < day)
        {
            return DateOrder.YearMonthDay;
        }

        return month < day ? DateOrder.MonthDayYear : DateOrder.DayMonthYear;
    }

    private static int Position(string pattern, char letter)
    {
        var index = pattern.IndexOf(letter, StringComparison.Ordinal);
        return index < 0 ? int.MaxValue : index;
    }
}
