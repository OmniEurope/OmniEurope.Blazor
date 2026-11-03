using System.Globalization;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The title of the period an <see cref="OmniScheduler"/> shows, in the patterns of its culture: the long date of a
/// day, the year and month of a month, and the first and last days of a week joined by the library's range text,
/// the start written without the year (and without the month, where the culture writes the month after the day)
/// when both ends share it: "2 au 8 novembre 2026", "30 novembre au 6 décembre 2026".
/// </summary>
internal static class SchedulerPeriodTitle
{
    private static readonly char[] Separators = [' ', ',', ' '];

    internal static string Of(OmniCalendarView view, DateOnly first, DateOnly last, CultureInfo culture, Func<string, string, string> range) => view switch
    {
        OmniCalendarView.Day => Write(first, culture.DateTimeFormat.LongDatePattern, culture),
        OmniCalendarView.Week => Week(first, last, culture, range),
        _ => Write(first, culture.DateTimeFormat.YearMonthPattern, culture)
    };

    private static string Week(DateOnly first, DateOnly last, CultureInfo culture, Func<string, string, string> range)
    {
        var full = Without(culture.DateTimeFormat.LongDatePattern, "dddd");
        var start = full;
        if (first.Year == last.Year)
        {
            start = Without(start, "yyyy");
            // The month leaves the start only where it follows the day ("d MMMM"): "MMMM d" would lose its month.
            if (first.Month == last.Month && start.IndexOf("MMMM", StringComparison.Ordinal) > start.IndexOf('d', StringComparison.Ordinal))
            {
                start = Without(start, "MMMM");
            }
        }

        return range(Write(first, start, culture), Write(last, full, culture));
    }

    // A pattern of one letter ("d", left of "d MMMM yyyy") is a standard format to .NET: "%d" is the custom one.
    private static string Write(DateOnly date, string pattern, CultureInfo culture) =>
        date.ToDateTime(TimeOnly.MinValue).ToString(pattern.Length == 1 ? "%" + pattern : pattern, culture);

    /// <summary>A pattern without one of its parts and the spaces and commas that went with it; a dot stays, it marks
    /// the day in German ("2.").</summary>
    private static string Without(string pattern, string part)
    {
        var index = pattern.IndexOf(part, StringComparison.Ordinal);
        if (index < 0)
        {
            return pattern;
        }

        var end = index + part.Length;
        var before = pattern[..index].TrimEnd(Separators);
        var after = pattern[end..].TrimStart(Separators);
        // A part that stood between two others leaves one space between them.
        return before.Length > 0 && after.Length > 0 ? $"{before} {after}" : before + after;
    }
}
