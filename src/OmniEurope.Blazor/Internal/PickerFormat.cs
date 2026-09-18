using System.Text;
using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// How the date and time pickers write and read their field. The date follows the order and the
/// separators of the culture's short date, always on two-digit days and months and four-digit years,
/// so a value keeps the same width whatever it is; the time is always on 24 hours, as the hour column
/// counts from 00 to 23. Reading accepts that shape, the culture's own short date, the ISO shape and,
/// last, the culture's lenient parser.
/// </summary>
internal static partial class PickerFormat
{
    internal static string DatePattern(CultureInfo culture)
    {
        var pattern = culture.DateTimeFormat.ShortDatePattern;
        pattern = DayRun().Replace(pattern, "dd");
        pattern = MonthRun().Replace(pattern, "MM");
        return YearRun().Replace(pattern, "yyyy");
    }

    internal static string TimePattern(bool seconds) => seconds ? "HH:mm:ss" : "HH:mm";

    internal static string DateTimePattern(CultureInfo culture, bool seconds) => $"{DatePattern(culture)} {TimePattern(seconds)}";

    /// <summary>The pattern spelled with the letters of the interface language, "jj/mm/aaaa" in French.</summary>
    internal static string Placeholder(string pattern, PickerLetters letters)
    {
        var text = new StringBuilder(pattern.Length);
        foreach (var character in pattern)
        {
            text.Append(character switch
            {
                'd' => letters.Day,
                'M' => letters.Month,
                'y' => letters.Year,
                'H' => letters.Hour,
                'm' => letters.Minute,
                's' => letters.Second,
                _ => character.ToString()
            });
        }

        return text.ToString();
    }

    internal static bool TryParseDate(string text, CultureInfo culture, out DateOnly date)
    {
        var trimmed = text.Trim();
        string[] formats = [DatePattern(culture), culture.DateTimeFormat.ShortDatePattern, "yyyy-MM-dd"];
        return DateOnly.TryParseExact(trimmed, formats, culture, DateTimeStyles.None, out date)
            || DateOnly.TryParse(trimmed, culture, DateTimeStyles.None, out date);
    }

    internal static bool TryParseTime(string text, CultureInfo culture, out TimeOnly time)
    {
        var trimmed = text.Trim();
        string[] formats = ["HH:mm", "H:mm", "HH:mm:ss", "H:mm:ss"];
        return TimeOnly.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out time)
            || TimeOnly.TryParse(trimmed, culture, DateTimeStyles.None, out time);
    }

    internal static bool TryParseDateTime(string text, CultureInfo culture, out DateTime moment)
    {
        var trimmed = text.Trim();
        string[] formats =
        [
            DateTimePattern(culture, seconds: false), DateTimePattern(culture, seconds: true),
            "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss"
        ];
        return DateTime.TryParseExact(trimmed, formats, culture, DateTimeStyles.None, out moment)
            || DateTime.TryParse(trimmed, culture, DateTimeStyles.None, out moment);
    }

    [GeneratedRegex("d+")]
    private static partial Regex DayRun();

    [GeneratedRegex("M+")]
    private static partial Regex MonthRun();

    [GeneratedRegex("y+")]
    private static partial Regex YearRun();
}

/// <summary>The letters a placeholder spells each part of a date or a time with.</summary>
internal readonly record struct PickerLetters(string Day, string Month, string Year, string Hour, string Minute, string Second)
{
    internal static PickerLetters From(Func<string, string> localize) => new(
        localize("PickerLetterDay"), localize("PickerLetterMonth"), localize("PickerLetterYear"),
        localize("PickerLetterHour"), localize("PickerLetterMinute"), localize("PickerLetterSecond"));
}
