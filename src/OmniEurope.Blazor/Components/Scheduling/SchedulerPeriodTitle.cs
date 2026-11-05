using System.Globalization;
using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The title of the period an <see cref="OmniScheduler"/> shows, in the patterns of its culture: the long date of a
/// day, the year and month of a month, and the first and last days of a week joined by the library's range text.
/// Both days of a week are written in full, without the weekday, and the start drops the words it shares with the
/// end: its shared tail where the culture writes the day first ("2 au 8 novembre 2026", "2 – 8 de noviembre de
/// 2026"), its shared head where it writes the year first ("2. – 2026. november 8."). Where the start shares as many
/// leading as trailing words with the end (English: "November 2 – 8, 2026"), the start drops its tail and the end
/// drops the head they share, so the month is written once. A week across a new year keeps both whole.
/// </summary>
internal static partial class SchedulerPeriodTitle
{
    private static readonly char[] Separators = [' ', ',', ' '];

    internal static string Of(OmniCalendarView view, DateOnly first, DateOnly last, CultureInfo culture, Func<string, string, string> range) => view switch
    {
        OmniCalendarView.Day => Write(first, culture.DateTimeFormat.LongDatePattern, culture),
        OmniCalendarView.Week => Week(first, last, culture, range),
        _ => Write(first, culture.DateTimeFormat.YearMonthPattern, culture)
    };

    private static string Week(DateOnly first, DateOnly last, CultureInfo culture, Func<string, string, string> range)
    {
        var pattern = WithoutWeekday(culture.DateTimeFormat.LongDatePattern);
        var end = Write(last, pattern, culture);
        var start = Write(first, pattern, culture);
        // Across a new year nothing is shared that the reader could take from the end alone.
        if (first.Year != last.Year)
        {
            return range(start, end);
        }

        var (shortStart, shortEnd) = Shorten(start, end);
        return range(shortStart, shortEnd);
    }

    private static string Write(DateOnly date, string pattern, CultureInfo culture) =>
        date.ToDateTime(TimeOnly.MinValue).ToString(pattern, culture);

    /// <summary>The long date pattern without its weekday and the spaces and commas around it.</summary>
    private static string WithoutWeekday(string pattern)
    {
        var index = pattern.IndexOf("dddd", StringComparison.Ordinal);
        return index < 0
            ? pattern
            : (pattern[..index].TrimEnd(Separators) + " " + pattern[(index + 4)..].TrimStart(Separators)).Trim(Separators);
    }

    /// <summary>The start of a week without the words it shares with the end, and the end too when the shared head and
    /// tail tie. The start keeps at least its first word, so a day that differs is never lost.</summary>
    private static (string Start, string End) Shorten(string start, string end)
    {
        var startWords = Words().Matches(start);
        var endWords = Words().Matches(end);
        var most = Math.Min(startWords.Count - 1, endWords.Count);
        var tail = 0;
        while (tail < most && startWords[startWords.Count - 1 - tail].Value == endWords[endWords.Count - 1 - tail].Value)
        {
            tail++;
        }

        var head = 0;
        while (head < most && startWords[head].Value == endWords[head].Value)
        {
            head++;
        }

        if (tail >= head)
        {
            var last = startWords[startWords.Count - 1 - tail];
            var shortStart = start[..(last.Index + last.Length)].TrimEnd(Separators);
            return tail == head && head > 0 && head < endWords.Count
                ? (shortStart, end[endWords[head].Index..])
                : (shortStart, end);
        }

        return (start[startWords[head].Index..], end);
    }

    [GeneratedRegex(@"\S+")]
    private static partial Regex Words();
}
